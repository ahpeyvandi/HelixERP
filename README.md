# HelixERP

Modular, multi-tenant ERP inspired by Odoo, built with Clean Architecture, .NET 8,
Entity Framework Core, SQL Server, Redis, YARP and a React shell.

- Single source of truth: `PROJECT_SPEC.md`
- Live status: `docs/PROJECT_STATUS.md`

## Current State

**Phase 2 - Frontend authentication is complete.**

- YARP gateway inside Shell with database-driven routes (`/modules/{moduleKey}/...`)
- JWT login, refresh-token rotation, logout and `/me`
- Odoo-style groups, access rights and record-rule infrastructure behind `IPermissionService`
- Module registry (IP + port) persisted in `ModuleRegistrations`
- File storage: metadata in SQL Server `Files`, binaries in the `erp_files` volume
- React 18 + TypeScript + Vite 6 + Tailwind CSS 4 shell with RTL layout, sidebar, header and dark mode
- Odoo-inspired login screen with token storage, silent refresh and server-side logout
- 199 passing tests (132 backend unit, 13 integration, 26 architecture, 28 frontend); build has 0 warnings/errors

SQL Server is intentionally **not** containerized: the SQL Server image is not reachable in
this environment due to sanctions. Only the connection string changes if the database later
moves to another server or a managed instance. No Nginx is used anywhere.

## Prerequisites

- .NET SDK 8.0 (newer SDKs can build `net8.0`; runtime 8.0 required to run tests)
- A local SQL Server instance reachable at `localhost` with Windows authentication
- Docker Desktop with Compose v2 (for Redis and the file/log volumes)
- Node.js 20+ and npm (frontend)

## Quick Start

```powershell
# 1. Redis + persistent volumes
cd docker
docker compose up -d
cd ..

# 2. Shell (applies migrations and seeds groups/admin on startup)
dotnet run --project src/Shell/HelixERP.Shell.Api

# 3. Frontend (second terminal)
cd src/Frontend/helix-shell
npm install
npm run dev

# 4. Frontend tests (Vitest + Testing Library)
npm test
```

| Service | URL |
|---|---|
| Shell API | `http://localhost:5080` |
| Shell health | `http://localhost:5080/health` |
| Frontend dev server | `http://localhost:5173` |
| Redis | `localhost:6379` |
| SQL Server | local Windows service, `localhost:1433` |

The Vite dev server proxies `/api` and `/modules` to `http://localhost:5080`.

### Default development login

- Login: `admin`
- Password: `Helix@12345`

These are **development defaults** from `appsettings.json`. Override `Bootstrap:AdminPassword`
and `Jwt:SigningKey` through environment variables or user secrets before sharing the system.

```powershell
$body = @{ login = 'admin'; password = 'Helix@12345' } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri http://localhost:5080/api/auth/login -Method Post `
  -ContentType 'application/json' -Body $body
$headers = @{ Authorization = "Bearer $($auth.tokens.accessToken)" }
Invoke-RestMethod -Uri http://localhost:5080/api/auth/me -Headers $headers
```

## API Overview

| Method | Route | Notes |
|---|---|---|
| `POST` | `/api/auth/login` | Returns access token, refresh token and user groups |
| `POST` | `/api/auth/refresh` | Rotates the refresh token |
| `POST` | `/api/auth/logout` | Revokes the supplied refresh token |
| `GET` | `/api/auth/me` | Current user, superuser flag and effective groups |
| `GET` | `/api/modules` | Any authenticated user |
| `POST` | `/api/modules` | Platform admin only |
| `PUT` | `/api/modules/{key}/endpoint` | Update host/port |
| `POST` | `/api/modules/{key}/activate` | Enable routing |
| `POST` | `/api/modules/{key}/deactivate` | Disable routing |
| `DELETE` | `/api/modules/{key}` | Remove registration |
| `POST` | `/api/files` | Multipart upload (`file`, optional `relatedEntity`, `relatedId`) |
| `GET` | `/api/files/{id}` | Download with range support |
| `GET` | `/api/files?relatedEntity=&relatedId=` | List metadata |
| `DELETE` | `/api/files/{id}` | Soft delete + physical delete |
| `*` | `/modules/{key}/{**path}` | YARP proxy to the registered module |

Gateway requests require a JWT. YARP strips the `/modules/{key}` prefix and adds trusted
`X-Helix-User-Id`, `X-Helix-User-Login`, `X-Helix-Groups` and `X-Helix-Superuser` headers;
incoming values of those headers are removed first.

## Configuration

| Key | Purpose | Default |
|---|---|---|
| `ConnectionStrings:HelixDb` | SQL Server connection | local `HelixERP` DB, Windows auth |
| `Jwt:SigningKey` | HS256 signing secret (>= 32 bytes) | dev-only value |
| `Jwt:AccessTokenMinutes` | Access-token lifetime | `60` |
| `Jwt:RefreshTokenDays` | Refresh-token lifetime | `14` |
| `Bootstrap:SeedOnStartup` | Seed groups/rights/admin | `true` |
| `Bootstrap:AdminPassword` | Password for the seeded admin | dev-only value |
| `FileStorage:RootPath` | Physical file root | `volumes/files`; container: `/var/helix/files` |
| `FileStorage:MaxFileSizeBytes` | Upload limit | `104857600` (100 MiB) |
| `Gateway:RefreshSeconds` | Route refresh interval | `30` |

## Database & Migrations

The connection string uses Windows authentication and trusts the local self-signed certificate:

```
Server=localhost;Database=HelixERP;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True
```

Applied migrations:

- `20261029060234_InitialCreate` (`ModuleRegistrations`)
- `20261029065018_Phase1SecurityAndFiles` (`Users`, `Groups`, `UserGroups`, `AccessRights`, `RecordRules`, `RefreshTokens`, `Files`)

```powershell
dotnet ef database update `
  --project src/Shell/HelixERP.Shell.Infrastructure `
  --startup-project src/Shell/HelixERP.Shell.Api
```

`ShellDbContextFactory` falls back to the local connection string when
`ConnectionStrings__HelixDb` is not set. The installed `dotnet-ef` 7.0.5 works but prints a
version warning; install 8.0.2 when NuGet is reachable.

## Infrastructure & Data Safety

```powershell
cd docker
docker compose up -d
```

`volume-init` creates the file/log volumes with UID 1654 as owner. Redis uses AOF persistence.

Persistent named volumes (fixed names, never project-prefixed): `erp_redis_data`, `erp_files`,
`erp_logs`. **Never run `docker compose down -v`.** A normal `docker compose down`, an image
rebuild or a container recreation keeps all volumes and therefore all data.

## Build & Test

```powershell
dotnet build HelixERP.sln
dotnet test HelixERP.sln --no-build
```

Test projects and coverage are described in `tests/README.md`.

```powershell
cd src/Frontend/helix-shell
npm install
npm run build   # tsc --noEmit && vite build
npm test        # vitest run (jsdom + Testing Library)
```

## Frontend Authentication

`src/Frontend/helix-shell` ships an Odoo-inspired login screen wired to the Shell:

- `POST /api/auth/login` stores the access and refresh tokens (`localStorage` when "remember me" is
  checked, otherwise `sessionStorage`) and routes the user to the dashboard.
- The session is restored on reload through `/api/auth/me`; an expired access token is refreshed
  silently 60 seconds before expiry (single-flight, retried every 30 s after network failures).
- Logout calls `/api/auth/logout` so the refresh token is revoked server-side, then clears the client
  session and returns to the login screen.
- `npm test` runs the Vitest + Testing Library suite (28 tests: session store, auth API client,
  AuthProvider, login page).

The preview server (`npm run preview`) serves the built bundle on `http://127.0.0.1:4173` and proxies
`/api` and `/modules` to the Shell, which is how the flow is verified in a browser.
## Architecture Rules

- `Domain` has no dependency on EF Core, ASP.NET Core, Application or Infrastructure.
- `Application` defines use cases and ports; it never references Infrastructure.
- `Shared` is a leaf project (Result/Error, clock, current-user abstractions).
- Modules are isolated from each other; `Shell` is the composition root and gateway.
- No Nginx: YARP inside Shell is the single entry point.
- Files: metadata in SQL Server, binaries in the `erp_files` volume.

## Dev Ports

| Service | Port |
|---|---|
| Shell | `5080` |
| Sales | `5081` |
| Inventory | `5082` |
| Accounting | `5083` |
| Frontend dev | `5173` |
| Frontend preview | `4173` |

Containers listen on `8080` internally; ports are published per module by Compose in later phases.