# HelixERP - Project Status

> Last Updated: 2026-11-01 09:00
> Current Phase: Phase 2 - Authentication (Local + IdentityServer 7 external login; next: Jalali calendar, i18n, LDAP provider, modules)

## Overall Progress
- [x] Solution + Clean Architecture skeleton
- [x] BuildingBlocks + Architecture Tests
- [x] Docker Compose + Named Volumes + local SQL Server
- [x] Entity Framework Core setup + First Migration
- [x] Shell (YARP + Auth + Odoo Permission System part 1 + Module Registry)
- [x] File Storage service (SQL table + volume)
- [x] IdentityServer 7 container (`src/Ids`) + OIDC external login with user/group mapping
- [ ] Sales Module
- [ ] Inventory Module
- [ ] Accounting Module
- [x] Frontend Shell: layout + Odoo-style login + JWT auth loop (React + TypeScript + Vite + Tailwind 4 + RTL + dark mode)
- [ ] Active Directory (LDAP provider)
- [ ] Multi-Company
- [ ] Document Sequences
- [ ] Chatter / Activity
- [ ] Background Jobs
- [ ] Holiday Calendar
- [ ] Cross-module integration tests

## Detailed Status

### 1. BuildingBlocks
- [x] Domain primitives: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Guard`, `DomainException`, `IDomainEvent`
- [x] Shared: `Result` / `Result<T>` / `Error`, `IClock`, `ICurrentUser` (now exposes display name and superuser flag)
- [x] Application contracts: commands/queries, `IUnitOfWork`, `IRepository`, domain events, `IPermissionService`, `IPasswordHasher`, `IFileStorage`
- [x] Infrastructure: `HelixDbContext`, UTC converters, decimal precision, domain-event dispatch, `SystemClock`
- [x] Platform aggregate `ModuleRegistration` (key/host/port validation, activation, domain event)
- [x] Security domain (Phase 1): `User`, `Group` (hierarchy), `UserGroup`, `AccessRight` (`Read`/`Write`/`Create`/`Unlink`), `RecordRule`, `RefreshToken`
- [x] External login mapping (Phase 2): `ExternalLogin` aggregate (`UserId`, `Provider`, `Subject`) with a unique index on (`Provider`, `Subject`); `User.LinkExternalLogin` / `UnlinkExternalLogin`
- [x] Platform file metadata: `FileAttachment` mapped to `Files`
- **Tests**: 68 xUnit tests (primitives, guard/result, module registration, domain events, DbContext behavior)
- **Last Test Run**: 2026-11-01 09:00 - 68 passed, 0 failed
- **Next Actions**: record-rule domain evaluator, access-right caching, outbox for domain events

### 2. Infrastructure & Docker
- [x] docker-compose with Redis + Volumes; SQL Server intentionally not containerized (sanctions)
- [x] Local SQL Server connection (Windows authentication, encrypted connection)
- [x] Fixed named volumes: `erp_files`, `erp_redis_data`, `erp_logs`
- [x] `volume-init` creates `erp_files`/`erp_logs` with app UID 1654
- [x] Redis AOF persistence verified across container recreation and `docker compose down` (without `-v`)
- [x] Compose contract tests: no `mssql` service, no `erp_mssql_data`, no port 1433
- [x] Migrations applied to local `HelixERP`: `20261029060234_InitialCreate`, `20261029065018_Phase1SecurityAndFiles`
- [x] File storage root configurable via `FileStorage:RootPath`; container image sets `/var/helix/files`
- **Verification**: local DB has both migrations, 1 user, 9 groups, 8 access rights, 0 registered modules, 1 soft-deleted file row from the end-to-end check, and the physical file was removed from the volume.
- **Connection string**: `Server=localhost;Database=HelixERP;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True`
- **Notes**: the local server requires TLS with a self-signed certificate, so `TrustServerCertificate=True`. The restricted agent sandbox cannot negotiate TLS; EF and runtime checks were executed in an elevated developer shell.
- **Next Actions**: add a startup health check for SQL/Redis; mount `erp_files` at `/var/helix/files` when the Shell service is added to Compose; log volume wiring

### 3. Shell + Security + Gateway
- [x] YARP inside Shell (no Nginx), dynamic routes built from `ModuleRegistrations`
- [x] Route contract: `/modules/{moduleKey}/{**catch-all}` -> registered `http://host:port` with `PathRemovePrefix`
- [x] Dynamic `IProxyConfigProvider` + `DatabaseProxyConfigProvider`; gateway reloads after registry changes and every 30 seconds
- [x] Gateway routes require authentication and forward trusted `X-Helix-User-Id`, `X-Helix-User-Login`, `X-Helix-Groups`, `X-Helix-Superuser` headers (incoming values are stripped first)
- [x] JWT auth (HS256) with issuer/audience/lifetime validation; refresh-token rotation and revoke (`/api/auth/login`, `/refresh`, `/logout`, `/me`)
- [x] Auth methods endpoint `GET /api/auth/methods` reports enabled methods (local / LDAP flag / OIDC)
- [x] OIDC external login: `Authentication` config section, `GET /api/auth/oidc/login` challenge, callback handler that exchanges the external identity for internal JWT + refresh tokens and redirects the SPA with tokens in the URL fragment; auto-provision, email linking, default group + `GroupClaimMappings`
- [x] Atomic refresh-token consumption with a `rowversion` concurrency token; reuse of a consumed (revoked) token revokes the whole session family (`RefreshToken.RowVersion`, `AuthService.RevokeFamilyAsync`)
- [x] Production startup guard: `ProductionSecurityGuard` fails fast when `Jwt:SigningKey` or `Bootstrap:AdminPassword` still use the development defaults (production only)
- [x] File ownership enforcement in `LocalFileStorage`: only the owner, a superuser, or a member of `base.group_system` can open/delete a file; a regular user's listing only returns their own files
- [x] PBKDF2-SHA256 password hashing (210,000 iterations, unique salts, fixed-time compare)
- [x] Odoo-style groups with parent inheritance; effective group closure resolved transitively
- [x] Access rights (`Read`/`Write`/`Create`/`Unlink`) and `IPermissionService` implementation
- [x] Record-rule storage, activation and filtering by model/group/operation; domain JSON is validated
- [x] Module Registry API: list/get/register/update endpoint/activate/deactivate/delete; admin-only mutations
- [x] Bootstrap seeding: 9 standard groups, 8 access rights, admin user with `base.group_user`, `base.group_erp_manager`, `base.group_system`
- [x] ProblemDetails middleware for domain/not-found/forbidden errors
- **Tests**: 73 Shell unit tests + 17 Shell integration tests (real HTTP, in-memory EF, fake upstream module through YARP)
- **Last Test Run**: 2026-11-01 09:00 - 90 passed, 0 failed
- **Manual verification**: started Shell against local SQL Server; login as `admin`, `/api/auth/me`, module register/list/delete, file upload/download/delete all succeeded
- **Next Actions**: permission checks per module/model at each endpoint; record-rule domain evaluator; refresh-token cleanup job; LDAP provider; HTTPS config for destinations

### 3.5 IdentityServer / External Login
- [x] `src/Ids/HelixERP.IdentityServer.Api` runs Duende IdentityServer 7.0.0 in a separate container (`identityserver` service, `ids.Dockerfile`)
- [x] In-memory OIDC client for the Shell (`helix-shell`, code flow + PKCE + offline access), identity resources (`openid`/`profile`/`email`/`groups`), test users (alice/bob)
- [x] Razor Pages login UI (`/Account/Login`) with cookie authentication; development signing credential persisted on the `erp_ids` volume
- [x] `ExternalLogin` mapping (`Provider` + `Subject`) to internal users; auto-provisioning defaults to `base.group_user` and applies `GroupClaim` mappings (e.g. `ERP_Admins` → `base.group_erp_manager`)
- **Tests**: 4 AuthService external-login unit tests + `DockerComposeContractTests` for the `identityserver` service/volume + `/api/auth/methods` integration test
- **Last Test Run**: 2026-11-01 09:00 - passed
- **Next Actions**: EF-backed IdentityServer storage + identity resources, external sign-out (single logout), license key wiring for production, wire the Shell/IDS pair in the running compose stack

### 4. File Storage
- [x] `Files` table with metadata (`Id`, `FileName`, `StorageKey`, `ContentType`, `SizeBytes`, related entity/id, uploader, timestamps, soft delete, `rowversion`)
- [x] Physical files stored under `FileStorage:RootPath` (`erp_files` volume in containers)
- [x] Upload/download/list/delete API (`/api/files`) with permission checks on `base.attachment`
- [x] Size limit (100 MB default), empty-file rejection, traversal-safe file names/storage keys, atomic temp-file move, DB/physical compensation on failure
- [x] Streaming read/write with async `FileStream` and `enableRangeProcessing` on download
- **Tests**: 9 unit tests + 5 integration tests (round trip, anonymous rejection, missing file, cross-user ownership, own-file listing)
- **Last Test Run**: 2026-10-31 12:00 - 14 passed, 0 failed
- **Next Actions**: multipart streaming reader (current implementation buffers the form), virus scanning hook, per-record access rules, physical purge job for soft-deleted rows

### 5. Sales Module
- [x] Project skeleton (Domain/Application/Infrastructure/Api) with `/health`
- [ ] First aggregates and use cases
- **Tests**: 0 (no domain logic yet)
- **Last Test Run**: n/a
- **Next Actions**: customer and sales-order aggregates, Jalali-aware sequence numbers, permission checks

### 6. Inventory Module
- [x] Project skeleton (Domain/Application/Infrastructure/Api) with `/health`
- [ ] First aggregates and use cases
- **Tests**: 0 (no domain logic yet)
- **Last Test Run**: n/a
- **Next Actions**: product/stock-move aggregates and inventory valuation boundaries

### 7. Accounting Module
- [x] Project skeleton (Domain/Application/Infrastructure/Api) with `/health`
- [ ] First aggregates and use cases
- **Tests**: 0 (no domain logic yet)
- **Last Test Run**: n/a
- **Next Actions**: chart of accounts, journal entries, fiscal-year handling

### 8. Frontend
- [x] `src/Frontend/helix-shell` React 18 + TypeScript + Vite 6 + Tailwind CSS 4
- [x] RTL layout (`dir="rtl"`, Persian labels, logical spacing utilities)
- [x] Premium sidebar + sticky header, responsive mobile drawer, dark mode with persisted preference
- [x] Odoo-inspired login screen: brand mark, username/password fields, caps-lock hint, show/hide password, remember me, inline validation, theme toggle
- [x] Login method awareness: fetches `/api/auth/methods` and shows an IdentityServer sign-in button when OIDC is enabled
- [x] OIDC callback handling: parses the dropped-in URL fragment tokens, persists the session, cleans the address bar, and reloads to bootstrap via `/me`
- [x] Auth loop wired to Shell (`/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`, `/api/auth/me`)
- [x] Session persistence: `localStorage` when "remember me" is checked, otherwise `sessionStorage`
- [x] Automatic silent refresh 60 s before access-token expiry (single-flight, 30 s retry after network failures)
- [x] Logout/session-return hardening: session version guard (`sessionVersion`) discards stale refresh, `/me`, and login responses after logout; a failed (network/server) refresh always reports failure (`runRefresh` returns `null`) and never hands out the expired access token
- [x] Session bootstrap on reload: `/me` validation, refresh when the access token is expired, logout when the refresh token is rejected
- [x] Header user menu with display name, effective group chips and a logout action that revokes the refresh token
- [x] Dashboard after login (no separate route yet) and production build (`tsc --noEmit && vite build`)
- [x] Dev/preview proxy `/api` and `/modules` to `http://localhost:5080`
- [ ] Jalali date picker, i18n switcher, Shadcn/ui component layer, password reset, route-level guards
- **Tests**: 34 frontend tests (7 session store, 6 auth API client, 9 AuthProvider, 8 login page, 4 OIDC callback)
- **Last Test Run**: 2026-11-01 09:00 - 34 passed, 0 failed
- **Manual verification**: headless Edge against the preview server: login card renders RTL, invalid credentials show a Persian message, tokens persist, forced token expiry triggers a silent refresh and rotation, user menu opens, logout clears storage and revokes the refresh token, dark mode applies; 0 console errors; no horizontal overflow at 1440x900 and 390x844
- **Next Actions**: Jalali calendar, i18n switcher, password reset, module pages, Vazirmatn font asset, design-system components

## Test Health
- Backend unit tests: 141 (68 BuildingBlocks + 73 Shell)
- Backend integration tests: 17 (Shell API, gateway proxying, files, auth races, auth methods; in-memory EF instead of Testcontainers)
- Architecture tests: 26 (namespace/assembly conventions, repository layout scanners, no-Nginx guard)
- Frontend tests: 34 (Vitest + Testing Library: session store, auth API client, AuthProvider, login page, OIDC callback)
- Total: 218 tests (184 backend + 34 frontend), 0 failed, 0 skipped
- Last Full Test Run: 2026-11-01 09:00 - backend 184 passed, frontend 34 passed
- Build: 0 warnings (except NuGet vulnerability-data probe under the restricted network), 0 errors (`dotnet build` and `tsc --noEmit && vite build`)
- Known Flaky Tests: none

## Known Issues
- **NetArchTest unavailable**: NuGet access is restricted; architecture rules are enforced by `ArchitectureProbe` (metadata type-reference analysis) plus probe self-tests.
- **Testcontainers unavailable**: integration tests use EF InMemory and a `TestServer` upstream; SQL Server/Redis container integration is still pending.
- **Record rules are infrastructure-only**: rules are stored, validated, filtered by group/operation and exposed through `IPermissionService`, but the Odoo-style domain evaluator/query translation is not implemented yet. Until then, file access is enforced with the hard-coded owner/superuser/`base.group_system` rule in `LocalFileStorage` (intended to be replaced by record-rule evaluation later).
- **Dev secrets**: `Jwt:SigningKey` and `Bootstrap:AdminPassword` in `appsettings.json` are development defaults. Override them via environment variables or user secrets before any shared/production deployment. `ProductionSecurityGuard` now fails startup in the `Production` environment if either still equals the defaults.
- **Bootstrap seeding**: enabled by default in `appsettings.json`; set `Bootstrap:SeedOnStartup=false` for controlled environments.
- **Access tokens cannot be revoked** before expiry (60 minutes default); only refresh tokens are revocable.
- **File upload buffering**: minimal-API multipart form reading buffers the request; the 100 MB limit is enforced by `FormOptions` and by the storage service.
- **Soft-deleted files** keep metadata rows and disappear from list/download; a purge job is pending.
- **EF tooling**: global `dotnet-ef` is 7.0.5; it applies EF Core 8 migrations with a version warning. Install 8.0.2 when NuGet is reachable. The `rowversion` concurrency column was added via a hand-written migration (`20261031070000_AddRefreshTokenConcurrency`) because the EF 7 tool cannot scaffold EF 8 migrations.
- **Encrypted local connection**: the agent sandbox cannot negotiate TLS with the local instance; use a normal/elevated developer shell for EF commands and runtime checks.
- **IdentityModel pin**: test projects explicitly pin `Microsoft.IdentityModel.Tokens` 8.1.2 and `Protocols.OpenIdConnect` 7.1.2 to unify assemblies referenced by `Microsoft.Data.SqlClient` and JWT bearer.
- **Frontend tokens**: access and refresh tokens live in Web Storage (`localStorage` with "remember me", otherwise `sessionStorage`). Simple and revocable, but readable by scripts; move to httpOnly cookies when a same-site BFF is introduced.
- **Frontend gaps**: no Jalali calendar, i18n switcher, password reset or per-route guards beyond the shell auth gate; the Vazirmatn font file is not bundled (fallback stack is configured).
- **IdentityServer**: `Duende.IdentityServer` 7.0.0 is used (the requested 6.x is absent from the offline cache). The container uses in-memory clients/resources/test users and a development signing credential persisted on the `erp_ids` volume; `RequireHttpsMetadata` in the Shell is `false` so the dev stack works over HTTP. A production Duende license key, real signing cert, and EF-backed stores are required before shared deployment.
- **Architecture scanner**: the no-Nginx source check now also scans `.ts`/`.tsx` while skipping `node_modules`, `bin`, `obj`, `dist` and other build/vendor folders.

## Architecture Decision Records
- AD001: No Nginx - YARP is used inside Shell
- AD002: Files stored via SQL `File` table + Docker volume `erp_files`
- AD003: SQL Server is a local installation under sanctions; moving later requires only a connection-string change
- AD004: Entity Framework Core Code First + Migrations
- AD005: Permission system follows Odoo model
- AD006: Project name is HelixERP
- AD007: Platform-wide concepts (`ModuleRegistration`, file metadata, security entities) live in `HelixERP.BuildingBlocks.Domain` because the specified solution has no `Shell.Domain` project
- AD008: Target `net8.0` with EF Core 8.0.2 (the stack allows .NET 8; versions are pinned to packages available offline)
- AD009: Fixed-name named volumes (`erp_redis_data`, `erp_files`, `erp_logs`) + `volume-init`; updates replace images/containers only, never volumes- AD010: Architecture rules are currently enforced by a metadata-based probe; migrate to NetArchTest when NuGet access is available
- AD011: SQL Server container and `erp_mssql_data` were removed from Docker Compose due to sanctions; the local SQL Server instance is the development database
- AD012: Authentication uses JWT HS256 access tokens + opaque refresh tokens stored as SHA-256 hashes; passwords use PBKDF2-SHA256 with 210,000 iterations (no external identity package needed)
- AD013: YARP gets its routes from a database-backed `IProxyConfigProvider`; registry mutations trigger an immediate reload and a 30-second refresh loop catches out-of-band changes
- AD014: Gateway prefix is `/modules/{moduleKey}`; YARP strips the prefix and forwards trusted user-context headers to modules
- AD015: Standard groups use Odoo-style technical ids (`base.group_system`, `sales.group_sales_user`, ...) and hierarchical parents for implied memberships
- AD016: Files use atomic temp-file writes, a traversal-safe storage key (`yyyy/MM/{guid}.{ext}`) and soft deletion in SQL
- AD017: Frontend pins React 18.3.1, Vite 6.4.5, TypeScript 5.9.3 and Tailwind CSS 4.3.3 (available in the offline npm cache); Tailwind 4 is configured CSS-first with `@theme`
- AD018: The SPA talks to `/api/auth/*` directly; the access token lives in Web Storage, refresh is single-flight with a 60-second skew, and logout revokes the refresh token server-side
- AD019: Frontend tests use Vitest 3 + Testing Library (jsdom) merged into `vite.config.ts`; `npm test` is the frontend test entry point
- AD020: Authentication methods are selectable via `Authentication` config (Local / LDAP / OIDC); the Shell is an OpenID Connect client and maps external identities (Issuer+Subject) to internal users through `ExternalLogin` (Provider+Subject unique), auto-provisioning with a default group + `GroupClaimMappings`
- AD021: Duende IdentityServer 7.0.0 runs in a dedicated container (`src/Ids` — the 6.x package is not in the offline cache) with in-memory stores + a persisted dev signing key on the `erp_ids` volume; production requires a Duende license key and EF-backed stores
