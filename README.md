# Badminton Queue Backend (API)

ASP.NET Core backend API for the Badminton Queue system. Provides authentication (JWT), players, queues, sessions, match tracking, and admin endpoints used by:

- `badminton-queue-web` (React)
- `badminton-queue-admin` (Java Swing admin desktop)

## Tech Stack

- .NET 9 (`net9.0`)
- ASP.NET Core Controllers
- EF Core 9 + Pomelo MySQL provider
- Swagger (Development only)
- JWT Bearer auth

## Prerequisites

- .NET SDK 9
- MySQL 8+ (or compatible)

## Quick Start (Local)

1) Start MySQL and create a database + user (example):

```sql
CREATE DATABASE badminton_db;
CREATE USER 'appuser'@'%' IDENTIFIED BY 'StrongPassword123!';
GRANT ALL PRIVILEGES ON badminton_db.* TO 'appuser'@'%';
FLUSH PRIVILEGES;
```

2) Configure the connection string and JWT settings.

- Default config is in `Api/appsettings.json`.
- Recommended: override locally via environment variables (see below).

3) Run the API:

```bash
dotnet run --project Api/Api.csproj
```

Default URLs (from `Api/Properties/launchSettings.json`):

- `http://localhost:5237`
- `https://localhost:7094`

Swagger UI (Development):

- `http://localhost:5237/swagger`

## Configuration

### Database

`Api/appsettings.json`:

- `ConnectionStrings:Default`

Override via environment variables:

- `ConnectionStrings__Default=server=localhost;port=3306;database=badminton_db;user=appuser;password=...;TreatTinyAsBoolean=true`

### JWT

`Api/appsettings.json`:

- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`

Override via env vars:

- `Jwt__Issuer=...`
- `Jwt__Audience=...`
- `Jwt__Key=...`

Security note: do not ship a hard-coded JWT key in production; inject it via secrets/env vars.

### CORS (Web Frontend)

The API enables a CORS policy named `web` and currently allows:

- `http://localhost:5173`

If you host the web app elsewhere, update CORS origins in `Api/Program.cs`.

## Database Migrations

- Migrations live in `Api/Migrations`.
- The API runs `db.Database.Migrate()` on startup (dev convenience). The database must exist and be reachable.

To create a new migration you typically need the EF CLI tool:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add YourMigrationName --project Api/Api.csproj --startup-project Api/Api.csproj
```

## Auth & Roles

- Auth is JWT Bearer; tokens are valid for ~8 hours (see `Api/Services/JwtService.cs`).
- Register (`POST /api/auth/register`) creates a `Player` role by default.
- Admin endpoints require `Role=Admin`.

Roles in this codebase:

- `Admin`
- `QueueMaster`
- `Player`

To create an Admin/QueueMaster account for local testing, you can either:

- Update the user role directly in the database, or
- Add a one-off seed/migration for your environment.

## Main Endpoints (Summary)

- Health: `GET /api/health`
- Auth: `POST /api/auth/login`, `POST /api/auth/register`
- Admin (Admin only):
  - `GET /api/admin/totals`
  - `GET /api/admin/history?take={take}&page={page}`
  - `GET /api/admin/users`
  - `POST /api/admin/users/{id}/soft-delete`
  - `POST /api/admin/users/{id}/restore`
- Players (auth required): `GET/POST /api/players`, `DELETE /api/players/{id}`
- Queues (auth required): create/manage queues, enqueue/remove, start/finish matches, history, ongoing matches
- Sessions (auth required): list/create/join/leave, details, member check-in/out, delete session

For the full and current contract, use Swagger in Development.

## Troubleshooting

- **DB connection errors**: verify `ConnectionStrings__Default` and that MySQL is running.
- **CORS blocked in browser**: add your web origin to the CORS policy in `Api/Program.cs`.
- **401/403**: missing/invalid token or insufficient role.
- **HTTPS issues**: use `http://localhost:5237` for local dev if you haven't trusted the dev certificate.
