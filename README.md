# CMS Events Service

A .NET service that ingests CMS webhook events, reconciles them by version, stores them in PostgreSQL, and exposes them through an authenticated REST API. Layered architecture (Domain / Application / Infrastructure / Api) with minimal APIs and separate EF Core read/write contexts.

## Running it

Everything is containerized, including the database - no local Postgres or manual schema setup, and it runs the same on macOS and Windows:

```bash
docker compose up --build
```

This starts Postgres, the API (on **http://localhost:8080**), and runs the test suite. To run only the app:

```bash
docker compose up --build db api
```

Health check: `GET http://localhost:8080/health`.

> Running from an IDE (F5) uses the ports in `launchSettings.json` (`7036`/`5025`), not `8080` - that port is the container's. See [Running outside Docker](#running-outside-docker) for the config it needs.

## Endpoints

| Method | Route | Role | Description |
|---|---|---|---|
| `POST` | `/cms/events` | `Cms` | Ingest a batch of events. Returns a per-outcome summary. |
| `GET` | `/consumers/cms/events` | `User` / `Admin` | List events. Admins also see disabled ones. |
| `DELETE` | `/consumers/cms/events/{id}` | `Admin` | Disable an event (override, doesn't touch CMS data). |

Auth is HTTP Basic. There are three roles with separate credentials: `Cms` (the webhook), `User`, and `Admin`.

## Credentials

Demo credentials, wired with defaults in `docker-compose.yml` so `docker compose up` just works. They're not real secrets and are checked in on purpose; override via `.env` if you want.

| Role | Username | Password |
|---|---|---|
| CMS | `cms-webhook` | `d1c47968-8c80-41de-97df-9c7e3ffb4bb1` |
| User | `reader-user@email.com` | `reader-secret` |
| Admin | `admin-user@email.com` | `admin-secret` |

## Event schema

`POST /cms/events` takes a JSON array. Every event has `type`, `id`, and `timestamp`. `payload` and `version` are only required for `publish` and `unpublish`; `delete` doesn't need them. `version` starts at 1.

- **publish**: create (first time seen) or update (only if the version is newer).
- **unpublish**: disable but keep the data. The event carries the payload/version, which also lets me handle the case where I never stored a published version before (I materialize it from the unpublish event, then mark it unpublished).
- **delete**: hard delete.

Unknown types are ignored instead of failing the batch, since the spec says event types are "not limited to" these.

```json
[
  { "type": "publish", "id": "X", "payload": { "name": "Erik" }, "version": 2, "timestamp": "2024-01-01T00:00:00Z" },
  { "type": "delete",  "id": "Y", "timestamp": "2024-01-01T00:00:00Z" }
]
```

The response reports what happened: `{ "applied", "ignored", "deleted", "failed" }`. A single bad event is counted as `failed` and never aborts the rest of the batch. Ready-made requests live in `Codebase/Api/Requests/*.http`.

## Tests

- **Unit** (`Tests/UnitTests`) - event processing rules (versioning, the unpublish corner case, delete) and authorization/validation logic.
- **Integration** (`Tests/IntegrationTests`) - full HTTP pipeline against a real Postgres via `WebApplicationFactory`, including Basic auth with valid and invalid credentials.

`docker compose up --build` runs the unit tests during the image build (a failure stops everything) and the integration tests at runtime. Integration results: `docker compose logs tests`. To see the unit test output (it's a build step, so BuildKit hides it on success):

```bash
docker compose build --no-cache --progress=plain tests
```

## Running outside Docker

The containerized setup injects all configuration for you. If you run things directly instead, the app and tests read empty defaults from `appsettings.json` on purpose - so you provide the missing values via **user-secrets** (dev-friendly and never committed).

**API with `dotnet run` / F5** - needs a Postgres connection string and the three sets of Basic Auth credentials. From `Codebase/Api`:

```bash
dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5432;Database=cms_events;Username=cms_app;Password=dev_password"
dotnet user-secrets set "Auth:Cms:Username" "cms-webhook"
dotnet user-secrets set "Auth:Cms:Password" "d1c47968-8c80-41de-97df-9c7e3ffb4bb1"
dotnet user-secrets set "Auth:User:Username" "reader-user@email.com"
dotnet user-secrets set "Auth:User:Password" "reader-secret"
dotnet user-secrets set "Auth:Admin:Username" "admin-user@email.com"
dotnet user-secrets set "Auth:Admin:Password" "admin-secret"
```

The API fails fast on startup if the connection string is missing.

**Unit tests** need no setup:

```bash
dotnet test Tests/UnitTests/CmsEvents.UnitTests.csproj
```

**Integration tests** need a reachable Postgres and a test connection string (credentials are injected automatically by the test host). From `Tests/IntegrationTests`:

```bash
dotnet user-secrets set "TEST_DATABASE_CONNECTION_STRING" "Host=localhost;Port=5432;Database=cms_events_tests;Username=cms_app;Password=dev_password"
dotnet test
```

## A few decisions worth calling out

**Synchronous processing.** I process events inside the request and only return `200` after they're persisted. The work is light and DB-bound, so a broker felt like overkill here - and a `200` becoming a clean confirmation for the CMS is a nicer contract. The processing lives in a use case, decoupled from the HTTP layer, so moving it to a background worker/outbox later is straightforward if volume ever justifies it.

**Version is the CMS's job, not mine.** I never generate or bump versions. An event is applied only if its version is newer than what I have; otherwise it's ignored. That gives idempotency and out-of-order tolerance for free, without a second source of truth.

**Two independent "disable" axes.** CMS `unpublish` (`IsUnpublishedByCms`) and admin disable (`IsDisabledByAdmin`) are separate on purpose. Admin disable is an operator override that hides an entity from regular users without affecting CMS data. It's intentionally one-way - the task asks to disable, not re-enable, so there's no enable endpoint.

**Read/write split.** Separate `ReadDbContext` (no-tracking reads) and `WriteDbContext`, with their own repositories.

**Schema on startup.** Outside the test environment I use EF Core's `EnsureCreated` to keep setup frictionless. In production I'd handle schema with real migrations instead.