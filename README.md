# Reservation System API

A reservation / appointment management REST API built with .NET 10 and a clean,
layered architecture. **All five phases are complete**: solution scaffolding,
infrastructure, the domain model, JWT authentication with refresh-token rotation
and role-based authorization, service-management CRUD, appointment booking with
conflict detection and status transitions, and a two-tier automated test suite
(unit + Testcontainers-backed integration).

> 🔗 **Live demo:** _TODO — add deployment link_
>
> 🖼️ **Screenshot:** _TODO — add Scalar API reference screenshot_

## Tech stack

| Concern         | Technology                                   |
| --------------- | -------------------------------------------- |
| Runtime         | .NET 10 (LTS), C# 14                          |
| Web framework   | ASP.NET Core Web API (Minimal APIs)          |
| Database        | PostgreSQL 17                                 |
| ORM             | Entity Framework Core 10 (Npgsql provider)   |
| API docs        | OpenAPI + [Scalar](https://scalar.com)       |
| Auth            | JWT bearer (access + refresh), BCrypt hashing |
| Validation      | FluentValidation                             |
| Logging         | Serilog (structured logging)                 |
| Containerization| Docker Compose (local PostgreSQL)            |
| Testing         | xUnit, FluentAssertions, Testcontainers      |

## Architecture

Clean, layered architecture. Dependencies point inward toward the domain:

```
ReservationSystem.Api            → HTTP entry point, DI, middleware, OpenAPI/Scalar
        │  ├── ReservationSystem.Application   → use cases, DTOs, service interfaces (skeleton)
        │  └── ReservationSystem.Infrastructure → EF Core, AppDbContext, configurations
        │             └── ReservationSystem.Application
        │             └── ReservationSystem.Domain
        └──────────────────────────────────────── ReservationSystem.Domain → entities, enums, rules
```

Project references:

- **Api** → Application, Infrastructure
- **Application** → Domain
- **Infrastructure** → Application, Domain
- **Tests** → Domain, Application, Infrastructure

### Domain model

- **User** — `Id`, `Email`, `PasswordHash`, `FullName`, `Role` (`Customer` / `BusinessOwner` / `Admin`)
- **Service** — `Id`, `Name`, `Description`, `DurationMinutes`, `Price`, `IsActive`
- **Appointment** — `Id`, `CustomerId` → User, `ServiceId` → Service, `StartTime`/`EndTime` (UTC), `Status` (`Pending` / `Confirmed` / `Cancelled` / `Completed`)
- **RefreshToken** — `Id`, `UserId` → User, `Token`, `ExpiresAt`, `RevokedAt`, `IsRevoked`

All entities derive from `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`).
Soft deletes are enforced via a global EF Core query filter; `CreatedAt` /
`UpdatedAt` are maintained automatically in `SaveChanges`.

## Project layout

```
reservation-api/
├── ReservationSystem.sln
├── docker-compose.yml
├── Directory.Build.props
├── src/
│   ├── ReservationSystem.Api/
│   ├── ReservationSystem.Application/
│   ├── ReservationSystem.Domain/
│   └── ReservationSystem.Infrastructure/
└── tests/
    └── ReservationSystem.Tests/
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for the local PostgreSQL instance)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`

## Configuration & secrets

**No passwords, connection strings, or secrets are committed to the repo.**
`appsettings.json` ships with empty placeholders. Provide real values locally via
one of the following (all are gitignored or stored outside the repo):

1. **`appsettings.Development.json`** (gitignored) — copy the provided template:
   ```bash
   cp src/ReservationSystem.Api/appsettings.Development.json.example \
      src/ReservationSystem.Api/appsettings.Development.json
   ```
2. **.NET user-secrets** (recommended for local dev):
   ```bash
   cd src/ReservationSystem.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
     "Host=localhost;Port=5432;Database=reservationdb;Username=reservation;Password=<your-password>"
   ```
3. **Environment variable**:
   ```bash
   export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=reservationdb;Username=reservation;Password=<your-password>"
   ```

Docker credentials are read from a gitignored `.env` file — copy the template:

```bash
cp .env.example .env   # then edit POSTGRES_PASSWORD
```

### Required JWT settings

The signing secret must come from a secure source (never committed). `Issuer`,
`Audience`, and token lifetimes have safe defaults in `appsettings.json`, but the
**`Jwt:Secret` must be supplied** via user-secrets or an environment variable.
The app fails fast on startup if the secret is missing.

```bash
# user-secrets (recommended)
cd src/ReservationSystem.Api
dotnet user-secrets set "Jwt:Secret" "<a-long-random-secret-at-least-32-characters>"

# …or environment variables
export Jwt__Secret="<a-long-random-secret-at-least-32-characters>"
export Jwt__Issuer="ReservationSystem"
export Jwt__Audience="ReservationSystem"
```

| Setting               | Meaning                              | Default        |
| --------------------- | ------------------------------------ | -------------- |
| `Jwt:Secret`          | HMAC-SHA256 signing key (≥ 32 chars) | _(required)_   |
| `Jwt:Issuer`          | Token issuer                         | ReservationSystem |
| `Jwt:Audience`        | Token audience                       | ReservationSystem |
| `Jwt:AccessTokenMinutes` | Access token lifetime (minutes)   | 15             |
| `Jwt:RefreshTokenDays`   | Refresh token lifetime (days)     | 7              |

> The provided `appsettings.Development.json.example` already includes a `Jwt`
> block with a placeholder secret for quick local setup.

## Authentication

Authentication is JWT-based and built directly on the project's own `User`
entity (no ASP.NET Core Identity). Passwords are hashed with **BCrypt**.

- **Access token** — short-lived (default 15 min) JWT carrying `sub` (user id),
  `email`, `name`, and `role` claims.
- **Refresh token** — long-lived (default 7 days), opaque, random, and stored in
  the database. **Rotated on every use**: exchanging a refresh token revokes it
  and issues a brand-new pair. Logout revokes the supplied refresh token.

Authorization uses role policies (`Customer`, `BusinessOwner`, `Admin`); new
registrations default to the `Customer` role.

### Endpoints

| Method | Route                | Auth        | Description                          |
| ------ | -------------------- | ----------- | ------------------------------------ |
| POST   | `/api/auth/register` | Public      | Create a Customer account            |
| POST   | `/api/auth/login`    | Public      | Authenticate, get a token pair       |
| POST   | `/api/auth/refresh`  | Public¹     | Rotate refresh token, get a new pair |
| POST   | `/api/auth/logout`   | Bearer      | Revoke a refresh token               |
| GET    | `/api/auth/me`       | Bearer      | Current user's profile               |

¹ Requires a valid (unexpired, unrevoked) refresh token in the body.

Errors are returned as RFC 7807 **ProblemDetails** with meaningful status codes
(`400` validation, `401` invalid credentials/token, `409` email already exists).

## Services

CRUD for bookable services. Reads are public; writes require the **BusinessOwner**
or **Admin** role (enforced by the `ManageServices` policy). Deletes are **soft**
(`IsDeleted = true`) and the row is retained but hidden by the global query filter.

| Method | Route                | Auth                  | Description                       |
| ------ | -------------------- | --------------------- | --------------------------------- |
| GET    | `/api/services`      | Public                | Paged list (active only default)  |
| GET    | `/api/services/{id}` | Public                | Single service                    |
| POST   | `/api/services`      | BusinessOwner / Admin | Create (201 + `Location`)         |
| PUT    | `/api/services/{id}` | BusinessOwner / Admin | Update                            |
| DELETE | `/api/services/{id}` | BusinessOwner / Admin | Soft delete (204)                 |

**List query parameters** (`GET /api/services`):

| Param            | Type   | Default | Notes                                          |
| ---------------- | ------ | ------- | ---------------------------------------------- |
| `page`           | int    | 1       | 1-based; values < 1 reset to 1                 |
| `pageSize`       | int    | 20      | Clamped to a max of **100**                    |
| `search`         | string | –       | Case-insensitive substring match on name       |
| `sortBy`         | enum   | `CreatedAt` | `CreatedAt` \| `Name` \| `Price`           |
| `sortDescending` | bool   | false   |                                                |
| `isActive`       | bool?  | –       | Omitted ⇒ active only; set `false` for inactive |

Responses use `PagedResult<T>` — `{ items, page, pageSize, totalCount, totalPages }`.

> **Getting a BusinessOwner account:** registration always creates a `Customer`.
> To manage services, promote a user directly in the database:
> ```bash
> docker exec -e PGPASSWORD=<pwd> reservation-postgres \
>   psql -U reservation -d reservationdb \
>   -c "UPDATE users SET \"Role\"='BusinessOwner' WHERE \"Email\"='you@example.com';"
> ```
> Then **log in again** so the new role is embedded in a fresh access token.

## Appointments

Booking flow for customers, with staff-managed lifecycle. All endpoints require
authentication; the booking customer is always taken from the **access token**,
never from the request body.

| Method | Route                            | Auth                  | Description                          |
| ------ | -------------------------------- | --------------------- | ------------------------------------ |
| POST   | `/api/appointments`              | Any authenticated     | Book a slot (201 + `Location`)       |
| GET    | `/api/appointments`              | Any authenticated     | Paged list (own only for customers)  |
| GET    | `/api/appointments/{id}`         | Owner / staff         | Single appointment                   |
| POST   | `/api/appointments/{id}/confirm` | BusinessOwner / Admin | `Pending → Confirmed`                |
| POST   | `/api/appointments/{id}/complete`| BusinessOwner / Admin | `Confirmed → Completed`              |
| POST   | `/api/appointments/{id}/cancel`  | Owner / staff         | `Pending`/`Confirmed → Cancelled`    |

**Business rules** (enforced server-side):

- **End time is computed** as `StartTime + Service.DurationMinutes` — clients send
  only `serviceId` and `startTime`.
- **No past bookings** — `StartTime` must be in the future (compared in UTC).
- **No overlaps** — a new booking is rejected (`409`) if it overlaps a
  non-cancelled appointment for the same service, where overlap means
  `newStart < existingEnd && newEnd > existingStart` (half-open ranges, so a
  booking starting exactly when another ends is allowed).
- **Inactive/deleted services** cannot be booked (`400`/`404`).
- **Status transitions** are validated; invalid moves (e.g. confirming a
  completed appointment) return `400`.
- **Visibility** — customers only ever see/act on their own appointments
  (`403` otherwise); BusinessOwner/Admin see and manage all.

**List query parameters** (`GET /api/appointments`): `page`, `pageSize` (max 100),
`status`, `serviceId`, `from`/`to` (UTC start-time range), `sortBy`
(`StartTime` | `CreatedAt` | `Status`), `sortDescending`.

### Concurrency / race conditions

Double-booking is prevented at two levels:

1. **Application** — the overlap check and the insert run inside a database
   **transaction**, giving a friendly `409` in the common case.
2. **Database** — a PostgreSQL **GiST exclusion constraint**
   (`ck_appointments_no_overlap`, via `btree_gist`) makes overlapping
   `[StartTime, EndTime)` ranges for the same active appointment *physically
   impossible*, even under two simultaneous requests that both pass the
   application check. A constraint violation is surfaced as a `409`.

This belt-and-braces approach means correctness does not depend on application
timing — the database is the final arbiter.

## How to run

1. **Start PostgreSQL** (reads `.env`):
   ```bash
   docker compose up -d
   ```

2. **Apply database migrations**:
   ```bash
   dotnet ef database update \
     --project src/ReservationSystem.Infrastructure \
     --startup-project src/ReservationSystem.Api
   ```

3. **Set the JWT secret** (see [Required JWT settings](#required-jwt-settings)):
   ```bash
   cd src/ReservationSystem.Api && dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)" && cd -
   ```

4. **Run the API**:
   ```bash
   dotnet run --project src/ReservationSystem.Api
   ```

5. **Open the docs** — the Scalar API reference is served in Development at:
   ```
   https://localhost:<port>/scalar/v1
   ```
   Click **Authentication** in Scalar, paste an access token under the `Bearer`
   scheme, and protected endpoints (`/api/auth/me`, `/api/auth/logout`) become
   callable. The raw OpenAPI document is at `/openapi/v1.json`, and the health
   check at `/health`.

### Try the auth flow with curl

```bash
BASE=http://localhost:5080   # match your launch URL

# 1) Register (returns accessToken + refreshToken)
curl -s -X POST $BASE/api/auth/register -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"Str0ng!Pass","fullName":"Alice Doe"}'

# 2) Login
curl -s -X POST $BASE/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"Str0ng!Pass"}'

# 3) Current user (use the accessToken from above)
curl -s $BASE/api/auth/me -H "Authorization: Bearer <ACCESS_TOKEN>"

# 4) Refresh (rotates the refresh token)
curl -s -X POST $BASE/api/auth/refresh -H "Content-Type: application/json" \
  -d '{"refreshToken":"<REFRESH_TOKEN>"}'

# 5) Logout (revokes the refresh token; requires the access token)
curl -s -X POST $BASE/api/auth/logout -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" -d '{"refreshToken":"<REFRESH_TOKEN>"}'
```

## Useful commands

```bash
# Build the whole solution
dotnet build

# Add a new migration
dotnet ef migrations add <Name> \
  --project src/ReservationSystem.Infrastructure \
  --startup-project src/ReservationSystem.Api

# Tear down the database container (keeps the volume)
docker compose down

# Tear down and delete data
docker compose down -v
```

## Testing

Two tiers of tests live in `tests/ReservationSystem.Tests`:

- **Unit tests** (`Unit/`) — fast, no I/O. Cover the pure booking rules (overlap
  with half-open intervals, end-time computation, status transitions), BCrypt
  password hashing, and the FluentValidation validators.
- **Integration tests** (`Integration/`) — exercise the real API over HTTP via
  `WebApplicationFactory<Program>` against a throwaway **PostgreSQL 17** container
  started with **Testcontainers**. Migrations (including the GiST overlap
  constraint) are applied, so the true schema is tested. Each test resets the
  database for isolation, so tests are deterministic and order-independent.

The integration suite includes a **real concurrency test**: two identical booking
requests are fired simultaneously and the suite asserts exactly one `201` and one
`409`, proving the exclusion constraint defeats the race condition.

### Running the tests

Docker Desktop must be running (Testcontainers starts a container). Then:

```bash
dotnet test
```

That builds everything and runs both tiers. To run only the fast unit tests:

```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

## Roadmap

- [x] **Phase 1** — Solution scaffolding, infrastructure, domain model
- [x] **Phase 2** — Authentication & authorization (JWT + refresh rotation, roles)
- [x] **Phase 3** — Service management CRUD (pagination, filtering, role-based access)
- [x] **Phase 4** — Appointment booking (conflict detection, status transitions, RBAC)
- [x] **Phase 5** — Tests (unit + Testcontainers integration, incl. concurrency)
