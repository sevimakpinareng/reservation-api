# Deployment guide

This API ships as a single container image and needs one managed PostgreSQL
database. The steps below are platform-agnostic — they map cleanly onto Render,
Railway, Fly.io, Azure Container Apps, AWS App Runner, Google Cloud Run, or a
plain VM with Docker.

## 1. Provision a PostgreSQL database

Create a **managed PostgreSQL 17** instance (most platforms offer a free tier).
Note its connection details: host, port, database name, username, password.

> The app needs the `btree_gist` extension (used by the appointment overlap
> constraint). It ships with standard PostgreSQL and is created automatically by
> the migration — no manual setup required on managed Postgres.

## 2. Build & publish the image

The repository contains a production `Dockerfile` (multi-stage, non-root,
listens on port **8080**).

```bash
docker build -t <registry>/reservation-api:latest .
docker push <registry>/reservation-api:latest
```

Most PaaS platforms can also build straight from the Git repo using the
`Dockerfile` — point them at the repository and they handle build + push.

## 3. Configure environment variables

Set these on the API service. **Never commit real secrets.**

| Variable                              | Required | Example / default                                                                 |
| ------------------------------------- | :------: | --------------------------------------------------------------------------------- |
| `ConnectionStrings__DefaultConnection`|   yes    | `Host=<db-host>;Port=5432;Database=reservationdb;Username=<user>;Password=<pwd>;SSL Mode=Require;Trust Server Certificate=true` |
| `Jwt__Secret`                         |   yes    | a long random string (>= 32 chars)                                                |
| `Jwt__Issuer`                         |   no     | `ReservationSystem`                                                               |
| `Jwt__Audience`                       |   no     | `ReservationSystem`                                                               |
| `Jwt__AccessTokenMinutes`             |   no     | `15`                                                                              |
| `Jwt__RefreshTokenDays`               |   no     | `7`                                                                              |
| `ASPNETCORE_HTTP_PORTS`               |   no     | `8080` (already set in the image)                                                 |
| `Database__MigrateOnStartup`          |   no     | `false` — see step 4                                                              |
| `ASPNETCORE_ENVIRONMENT`              |   no     | `Production` (set `Development` only if you want the Scalar UI exposed)            |

Generate a strong JWT secret:

```bash
openssl rand -base64 48
```

Managed Postgres usually requires TLS — append `SSL Mode=Require;Trust Server Certificate=true`
to the connection string (or `SSL Mode=VerifyFull` with the provider's CA).

## 4. Apply database migrations

Two supported strategies — pick one:

- **Recommended (separate step):** keep `Database__MigrateOnStartup=false` and run
  migrations as a one-off release/job command using the EF CLI against the
  production connection string:
  ```bash
  dotnet ef database update \
    --project src/ReservationSystem.Infrastructure \
    --startup-project src/ReservationSystem.Api
  ```
  This keeps schema changes explicit and avoids races when scaling to multiple
  instances.

- **Simple (auto-migrate):** set `Database__MigrateOnStartup=true`. The API applies
  any pending migrations on startup. Convenient for single-instance deployments
  and the Docker Compose demo; avoid it when running multiple replicas.

## 5. Deploy & verify

Expose the service's port `8080` (the platform typically fronts it with HTTPS).
Then check health:

```bash
curl https://<your-app-url>/health
# {"status":"Healthy","checks":[{"name":"postgres","status":"Healthy"}]}
```

The `/health` endpoint verifies database connectivity, so a `Healthy` response
confirms the API reached PostgreSQL successfully.

## 6. Smoke test

```bash
BASE=https://<your-app-url>
curl -X POST $BASE/api/auth/register -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"Str0ng!Pass","fullName":"You"}'
```

A `200` with an access/refresh token pair means the full stack (API + database +
auth) is live.
