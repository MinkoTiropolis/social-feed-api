# social-feed-api

A JSON API backend for a Twitter-like social feed, built as a technical assignment.
The frontend is imagined as a React single-page app and is not part of this repository.

## Stack

| Layer | Choice |
|---|---|
| Runtime | .NET 8 (LTS) |
| API | ASP.NET Core Web API, controllers |
| ORM | Entity Framework Core 8 |
| Database | SQL Server 2022 (Docker) |
| Auth | JWT access token + database-persisted, revocable refresh token |
| Docs | Swagger / OpenAPI |
| Tests | xUnit |

## Running it

Requires Docker and the .NET 8 SDK.

```bash
docker compose up -d
dotnet run --project src/SocialFeed.Api
```

The API starts on `http://localhost:5242` and opens Swagger at `/swagger`.

On startup it applies any pending migrations and, if the database is empty, seeds it
with sample users, posts and likes — so the feed is not blank on first run. A real
deployment would apply migrations as a separate step rather than letting the
application change its own schema; it is done here so a reviewer needs two commands
rather than four.

To inspect the database directly, connect any SQL client to `localhost,1433` with
user `sa`, password `LocalDev_Passw0rd!`, and *Trust server certificate* enabled.

`docker compose down` keeps the data; `docker compose down -v` wipes it so the next
run re-seeds from scratch.

## Seeded accounts

| Email | Password | Role | Status |
|---|---|---|---|
| `admin@sharebook.local` | `Admin123!` | Superuser | Approved |
| `daniel@sharebook.local` | `User123!` | User | Approved |
| `maria@sharebook.local` | `User123!` | User | Approved |
| `pending@sharebook.local` | `User123!` | User | **Pending** |

The last account is left pending on purpose: it lets you see a login rejected for
approval reasons, then approve it as the superuser, without registering first.

## Endpoints

| Method | Route | Access |
|---|---|---|
| POST | `/auth/register` | public |
| POST | `/auth/login` | public |
| POST | `/auth/refresh` | public |
| POST | `/auth/logout` | authenticated |
| GET | `/admin/users/pending` | superuser |
| POST | `/admin/users/{id}/approve` | superuser |
| GET | `/me` | authenticated |
| PATCH | `/me` | authenticated |
| GET | `/feed` | authenticated |
| POST | `/posts` | authenticated |
| GET | `/posts/{id}` | authenticated |
| POST | `/posts/{id}/like` | authenticated |
| DELETE | `/posts/{id}/like` | authenticated |
| GET | `/posts/{id}/likes` | authenticated |
| DELETE | `/posts/{id}` | author or superuser |
| POST | `/posts/{id}/restore` | author or superuser |

## Tests

```bash
dotnet test
```

The SQL Server container must be running first. The integration tests boot the real
application through `WebApplicationFactory` against their own database
(`SocialFeed_IntegrationTests`), dropped and recreated per run, so they exercise the
same migrations, cascades and query filters as production rather than a substitute
provider that would pass without proving anything.

**Unit tests** cover the retention boundary — a post soft deleted 11 and 10 days ago is
purged, 9 days is not — using an injected fixed clock. That rule is the reason
`TimeProvider` is injected rather than calling `DateTime.UtcNow`; otherwise verifying it
would mean waiting ten days. They also assert that purging a post removes its likes,
which is a foreign-key rule rather than anything the service does.

**Integration tests** cover the full journey — register, blocked as pending, superuser
approves, login succeeds, create a post, like it, see it in the feed, soft delete it,
see it excluded, restore it, see it return — plus that a user cannot delete someone
else's post, that the feed rejects anonymous callers, and that logging out stops the
refresh token from working.

## Design decisions

### Two origins, so bearer tokens rather than cookies

The brief mentions the API at `api.somedomain.com` and the frontend at
`app.somedomain.com`. Those are **different origins**, so the browser will not let the
frontend call the API unless the API opts in. CORS is configured with named origins
read from `appsettings.json` — never `AllowAnyOrigin`, which would let any site on the
internet call the API with a user's credentials.

This also drove the authentication choice. A cookie-based session across two
subdomains needs `SameSite=None; Secure`, third-party cookie handling, and
`AllowCredentials` on the CORS policy — increasingly fragile as browsers tighten
third-party cookie rules. Bearer tokens in an `Authorization` header avoid the problem
rather than working around it.

### Logout actually revokes something

A JWT is stateless: once issued it stays valid until it expires, so an endpoint that
only returns `200 OK` has not logged anybody out. Refresh tokens are therefore stored
in the database, and logout marks the row revoked. The session cannot be extended, and
the short access token lifetime (15 minutes) closes the remaining window.

Refresh tokens are stored as a SHA-256 hash, never in plain text — read access to the
database yields no usable session. A fast hash is correct here rather than a password
hash: the token is 32 bytes of randomness and is not guessable.

### The feed uses keyset pagination, not offset

The brief says the feed is "rendered in a list with infinite scroll, so make sure you
design it in an appropriate way". With `OFFSET/FETCH`, a post created while someone is
scrolling shifts every later row down one, so the next page re-serves an item the
reader has already seen and eventually skips another.

The feed instead seeks from the last item the client actually received, encoded as an
opaque cursor over `(CreatedAt, Id)`. Both parts matter: two posts can share a
timestamp, and without the id tiebreak one of them is skipped or duplicated at a page
boundary. `IX_Posts_DeletedAt_CreatedAt_Id` matches that ordering, so a page is an
index seek rather than a sort of the whole table.

Demonstrated: request page 1, create a new post, then request page 2 with the cursor —
the pages do not overlap. Offset paging would have repeated a post.

### Soft delete is a timestamp, not a flag

`Posts.DeletedAt` is a nullable timestamp rather than an `IsDeleted` bit, because the
retention rule is expressed in time: a background job hard-deletes posts soft-deleted
more than ten days ago. A boolean cannot answer "how long ago".

A global query filter hides deleted posts from every query in the application, so the
feed, the profile totals and every read endpoint exclude them without mentioning it.
Only two places opt out with `IgnoreQueryFilters()` — restore and the purge job — and
both need to find precisely the rows the filter hides.

### Structure

`Api → Services → Data`. Controllers bind a DTO, call a service and map the result to a
status code; services hold the rules and own the DTOs; `Data` holds entities, the
`DbContext`, configurations and migrations. `Api` also references `Data` so
`Program.cs` can register the `DbContext` — the one edge that is not strictly
one-directional.

Interfaces exist where something is genuinely swappable or needs a test seam, not as a
reflex. Most services are registered as concrete types.

## Security notes

- Passwords are hashed with `PasswordHasher<T>` (PBKDF2, per-user salt) and never
  appear in any response.
- Every ownership rule is enforced server-side. Only a post's author or a superuser can
  delete or restore it; a non-author gets `403`.
- Request DTOs are explicit and shaped to prevent mass assignment. `UpdateMeRequest` has
  no email, password, role or status property, so those cannot be changed through
  `PATCH /me` even if a client sends them.
- Login rejects an unknown email and a wrong password identically (`401`), so the
  endpoint cannot be used to discover who has an account. A pending account returns
  `403` with `"code": "account_pending"` — a deliberate trade, see assumptions.
- Errors use RFC 7807 `ProblemDetails` throughout.

### About the committed configuration values

`appsettings.json` contains a local SQL Server password and a JWT signing key. These
are **local development values for a throwaway container**, committed deliberately so
the project runs from a clean clone with no setup. Both are read through
`IConfiguration`, so a real deployment overrides them with the `ConnectionStrings__SocialFeed`
and `Jwt__Key` environment variables — in Azure, from App Configuration or Key Vault.
Nothing production-sensitive is in this repository.

## Assumptions

- Soft-deleted posts are excluded from `totalPosts` and `totalLikes`. A deleted post
  should not inflate your statistics.
- Users may like their own posts, consistent with Twitter.
- Like and unlike are idempotent: repeating either succeeds rather than erroring.
- The feed is global and chronological. No follow graph was specified, and the designs
  show no following UI.
- A post can be restored only within the ten-day retention window; after that the row
  is gone.
- Rejecting a pending login with a distinct code leaks a small signal that an address
  is registered — but only to someone who already knows the password. That is worth it
  for a frontend that can say "waiting for approval" instead of "wrong password".
- `PATCH /me` treats an omitted or null field as "leave unchanged", so there is no way
  to clear a description back to empty.
- **No edit endpoint for posts exists.** This is required by the brief, not an omission.
- Profile pictures are stored as a URL set through `PATCH /me`. File upload was left
  out deliberately — see below.

## What I would do next

- **Profile picture upload** behind an `IFileStorage` abstraction with a content-type
  allowlist, a size cap and server-generated filenames; the local disk implementation
  swaps for Azure Blob Storage without touching callers. The API currently accepts a
  URL, which satisfies "keep their profile picture" without a file-handling subsystem.
- **Rate limiting** on `/auth/login` and `/auth/register` using the built-in .NET 8
  limiter, to make credential stuffing expensive.
- **A global exception handler** so unhandled errors return `ProblemDetails` rather
  than a framework error page.
- **Deploying to Azure**: App Service or Container Apps for the API, Azure SQL for the
  database, Key Vault for the signing key, and the purge worker as a timer-triggered
  Azure Function so it runs once rather than in every instance. The health probe App
  Service expects would be the reason to add a `/health` endpoint, which is why there
  is not one here.
