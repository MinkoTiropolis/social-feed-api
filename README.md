# social-feed-api

A JSON API backend for a Twitter-like social feed, built as a technical assignment.
The frontend is imagined as a React single-page app and is not part of this repository.

## Stack

| Layer | Choice |
|---|---|
| Runtime | .NET 8 (LTS) |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Database | SQL Server 2022 (Docker) |
| Auth | JWT access token + database-persisted, revocable refresh token |
| Docs | Swagger / OpenAPI |
| Tests | xUnit |

## Status

Work in progress. This README is expanded in the final milestone with setup
instructions, seeded credentials, design decisions and assumptions.

## Getting started

```bash
docker compose up -d
dotnet run --project src/SocialFeed.Api
```

Full instructions follow once the compose file and solution are in place.

## Configuration

No secrets are committed to this repository. The database connection string and
the JWT signing key are supplied through user-secrets in development and through
environment variables elsewhere; `appsettings.json` carries placeholders only.
