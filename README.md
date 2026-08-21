# Social-feed-api

A JSON API for a Twitter-like social feed, built as a technical assignment. The frontend is
imagined as a React app and is not part of this repository.

## What it does

New users register and are sandboxed — they cannot log in until a superuser approves them.
Approved users post (up to 280 characters), like and unlike, read a paginated feed of
everyone's posts, and edit their own profile.

Posts cannot be edited, only deleted, and deletion is reversible: a deleted post disappears
from the API immediately but stays in the database, restorable for ten days. After that a
background job removes it.

## How it works

| | |
|---|---|
| Stack | .NET 8, ASP.NET Core (controllers), EF Core 8, SQL Server 2022 in Docker, Swagger, xUnit |
| Layers | `Api` (controllers) → `Services` (rules, DTOs) → `Data` (entities, `DbContext`, migrations) |
| Auth | 15-minute JWT access token, plus a refresh token stored in the database so logout can revoke it |
| Soft delete | Nullable `DeletedAt` plus an EF global query filter, so deleted posts vanish from every query |
| Feed | Keyset pagination over an opaque `(CreatedAt, Id)` cursor |
| Background job | Purges posts past the retention window and refresh tokens that can no longer be used |

Controllers bind a DTO, call a service and map the result to a status code — no EF or business
rules in a controller. Services are registered against interfaces (`IPostService` → `PostService`),
so controllers and the worker depend on abstractions.

## Running it

Requires Docker and the .NET 8 SDK. Nothing to configure.

```bash
docker compose up -d
dotnet run --project src/SocialFeed.Api
```

Then open **<http://localhost:5242/swagger>**.

On startup the API applies pending migrations and seeds an empty database, so the feed has
content on the first run.

SQL Server needs 20–30 seconds after `compose up` before it accepts connections. `docker compose
down` keeps your data; `down -v` wipes it so the next run re-seeds.

To browse the database, connect any SQL client to `localhost,1433` as `sa` with password
`LocalDev_Passw0rd!` and *Trust server certificate* enabled.

## Seeded accounts

| Email | Password | Role | Status |
|---|---|---|---|
| `admin@sharebook.local` | `Admin123!` | Superuser | Approved |
| `daniel@sharebook.local` | `User123!` | User | Approved |
| `maria@sharebook.local` | `User123!` | User | Approved |
| `pending@sharebook.local` | `User123!` | User | **Pending** |

The last one is left pending on purpose, so you can see a login rejected for approval reasons
and then approve it as the superuser without registering anything.

## Using the API

**Authenticate first.** Call `POST /auth/login` with a seeded account — Swagger pre-fills a
placeholder example, so replace it. Copy `accessToken` from the response, click **Authorize**,
and paste the token alone; Swagger adds the `Bearer` prefix itself. Tokens last 15 minutes;
after that, log in again or call `POST /auth/refresh`.

| Method | Route | Access |
|---|---|---|
| POST | `/auth/register` | public — creates a **pending** account |
| POST | `/auth/login` | public — returns access + refresh token |
| POST | `/auth/refresh` | public — new access token |
| POST | `/auth/logout` | authenticated — revokes the refresh token |
| GET | `/admin/users/pending` | superuser |
| POST | `/admin/users/{id}/approve` | superuser |
| GET | `/me` | authenticated — profile plus `totalPosts` and `totalLikes` |
| PATCH | `/me` | authenticated — name, description, picture URL only |
| GET | `/feed` | authenticated — `?cursor=…&pageSize=20` |
| POST | `/posts` | authenticated |
| GET | `/posts/{id}` | authenticated |
| POST / DELETE | `/posts/{id}/like` | authenticated — both idempotent |
| GET | `/posts/{id}/likes` | authenticated — `?page=1&pageSize=20` |
| DELETE | `/posts/{id}` | author or superuser — soft delete |
| POST | `/posts/{id}/restore` | author or superuser |

**Paging the feed.** Call `/feed` with no cursor for the first page. The response carries
`nextCursor` and `hasMore`; pass `nextCursor` back as `?cursor=…` for the next page. On the last
page `hasMore` is false and `nextCursor` is null. Default size 20, maximum 100, larger values are
clamped rather than rejected. Treat the cursor as an opaque string.

**Errors** are RFC 7807 `ProblemDetails` throughout. Validation failures add an `errors` object
keyed by field; a pending login adds `"code": "account_pending"` so the client can tell it apart
from bad credentials.

## Tests

```bash
dotnet test
```

The container must be running. Unit tests cover the retention boundary — 11 and 10 days are
purged, 9 is not — using an injected fixed clock, plus cursor encoding and the cascade that
removes a purged post's likes. Integration tests boot the real application through
`WebApplicationFactory` against their own database, and walk the full journey: register →
blocked → approved → login → post → like → feed → delete → excluded → restore → visible.

## Design decisions

**Two origins, so bearer tokens.** The brief puts the API at `api.somedomain.com` and the
frontend at `app.somedomain.com` — different origins, so CORS is configured with named origins
from configuration, never `AllowAnyOrigin`. It also drove the auth choice: a cookie session
across subdomains needs `SameSite=None; Secure` and `AllowCredentials`, while a bearer token in
an `Authorization` header avoids the problem entirely.

**Logout revokes something.** A JWT stays valid until it expires, so a logout endpoint that just
returns `200` has logged nobody out. Refresh tokens live in the database and logout marks the row
revoked, so the session cannot be extended; the 15-minute access token closes the rest of the gap.
Tokens are stored as SHA-256 hashes, so database access yields no usable session.

**Keyset pagination, not offset.** With `OFFSET/FETCH`, a post created while someone scrolls
shifts every later row down, so the next page repeats an item already seen. The feed instead seeks
from the last post the client received, using `(CreatedAt, Id)` — the id breaks ties when two posts
share a timestamp. `IX_Posts_DeletedAt_CreatedAt_Id` matches that order, so a page is an index seek.

**Soft delete is a timestamp.** `DeletedAt` is nullable rather than a bool, because the rule is
expressed in time: posts deleted more than ten days ago are removed for good, and a bool cannot
answer "how long ago". Only restore and the purge job opt out of the global filter.

**Likers are a separate endpoint.** A post carries `likeCount` and `likedByMe` — what the card
renders — while the list of who liked it is paginated at `/posts/{id}/likes`, fetched when someone
asks. Embedding it would put every liker of every post into each feed page.

## Configuration

Settings live in `src/SocialFeed.Api/appsettings.json`; environment variables override them, with
`__` in place of `:` (`Jwt__Key`, `ConnectionStrings__SocialFeed`). Retention days, purge interval,
token lifetimes and allowed CORS origins are all configurable.

The committed SQL password and JWT key are **local development values for a throwaway container**,
committed deliberately so the project runs from a clean clone with no setup. A real deployment
overrides both through environment variables — in Azure, from Key Vault. Nothing production
sensitive is in this repository.

## Assumptions

- Soft-deleted posts are excluded from `totalPosts` and `totalLikes`.
- Users may like their own posts; like and unlike are idempotent.
- The feed is global and chronological — no follow graph was specified, and the designs show none.
- A post is restorable only within the ten-day window.
- Rejecting a pending login with a distinct code leaks a small signal that an address is
  registered, but only to someone who already has the password. Worth it for clearer UX.
- `PATCH /me` leaves omitted or null fields unchanged, so a description cannot be cleared.
- **No edit endpoint for posts exists** — required by the brief, not an omission.
- Profile pictures are a URL set through `PATCH /me`; file upload was left out deliberately.

## What I would do next

- **Profile picture upload** behind an `IFileStorage` abstraction, swapping local disk for Azure
  Blob Storage without touching callers.
- **Rate limiting** on the auth endpoints using the built-in .NET 8 limiter.
- **Azure deployment**: App Service for the API, Azure SQL, Key Vault for the signing key, and the
  purge worker as a timer-triggered Function so it runs once rather than per instance — which is
  also where a `/health` endpoint would earn its place.
