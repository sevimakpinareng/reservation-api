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
| `PORT`                                |   no     | If the platform injects it (e.g. Render), the app binds to it automatically       |
| `Database__MigrateOnStartup`          |   no     | `false` — see step 4                                                              |
| `ASPNETCORE_ENVIRONMENT`              |   no     | `Production` (set `Development` only if you want the Scalar UI exposed)            |

> **Port binding:** the image listens on `8080` by default. If the platform
> injects a `PORT` environment variable (Render, Heroku, Cloud Run), the app
> detects it and binds to that port automatically — no extra configuration needed.

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

---

## Deploying to Render

[Render](https://render.com) builds the `Dockerfile` straight from GitHub and
offers free managed PostgreSQL — a good zero-cost target. The repo includes a
`render.yaml` **Blueprint**, so the whole stack provisions in one click.

### Option A — One-click Blueprint (recommended)

1. **Push the repo to GitHub** (see the project README for the remote/push
   commands).

2. In the Render dashboard: **New → Blueprint**, then select this repository.
   Render reads [`render.yaml`](render.yaml) and shows a plan: a free PostgreSQL 17
   database (`reservation-db`) and a Docker web service (`reservation-api`), both in
   Frankfurt.

3. Click **Apply**. Render then automatically:
   - provisions the database,
   - wires `DATABASE_URL` into the web service (the app converts it to an Npgsql
     connection string on startup),
   - generates a strong random `Jwt__Secret`,
   - sets `Database__MigrateOnStartup=true` so the schema is created on first boot,
   - injects `PORT`, which the app binds to automatically.

   No manual environment variables are required.

4. **Verify:** open `https://<your-service>.onrender.com/health` — it should report
   `Healthy`. Then run the smoke test above against that URL.

### Option B — Manual setup (fallback)

1. **Create the database.** Render dashboard → **New → PostgreSQL**, free plan,
   PostgreSQL **17**, region Frankfurt. Copy its **Internal Database URL**.

2. **Create the web service.** **New → Web Service** → connect the repo. Render
   auto-detects the `Dockerfile`; set **Runtime:** Docker, **Branch:** `main`,
   **Region:** same as the database.

3. **Set environment variables** (you do **not** need `PORT` —
   it is injected and bound automatically):

   | Key                          | Value                                                          |
   | ---------------------------- | -------------------------------------------------------------- |
   | `DATABASE_URL`               | the database's Internal Database URL (`postgresql://...`)       |
   | `Jwt__Secret`                | output of `openssl rand -base64 48`                            |
   | `Database__MigrateOnStartup` | `true`                                                         |
   | `ASPNETCORE_ENVIRONMENT`     | `Production` (use `Development` to expose the Scalar UI)        |

   > Prefer `DATABASE_URL` — the app converts it for you. If you'd rather provide a
   > native connection string instead, set `ConnectionStrings__DefaultConnection`
   > to `Host=HOST;Port=5432;Database=DB;Username=USER;Password=PWD;SSL Mode=Require`.

4. **Deploy & verify** as in Option A, step 4.

> Free Render web services sleep after inactivity and cold-start on the next
> request; the first call after idling may take a few seconds.
