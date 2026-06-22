# Reservation System API

A reservation / appointment management REST API built with .NET 10 and a clean,
layered architecture. **This is Phase 1**: solution scaffolding, infrastructure,
and the domain model only. Authentication, business logic, and tests arrive in
later phases.

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
| Logging         | Serilog (structured logging)                 |
| Containerization| Docker Compose (local PostgreSQL)            |

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

3. **Run the API**:
   ```bash
   dotnet run --project src/ReservationSystem.Api
   ```

4. **Open the docs** — the Scalar API reference is served in Development at:
   ```
   https://localhost:<port>/scalar/v1
   ```
   The raw OpenAPI document is at `/openapi/v1.json`, and the health check at
   `/health`.

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

## Roadmap

- [x] **Phase 1** — Solution scaffolding, infrastructure, domain model
- [ ] **Phase 2** — Authentication & authorization (JWT, roles)
- [ ] **Phase 3** — Business logic & API endpoints
- [ ] **Phase 4** — Tests (unit & integration)
