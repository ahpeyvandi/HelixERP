# Tests

| Project | Purpose | Status |
|---|---|---|
| `HelixERP.BuildingBlocks.UnitTests` | Domain/shared/application/infrastructure primitives | Active - 68 tests |
| `HelixERP.Shell.UnitTests` | Security domain, permissions, JWT, module registry, file storage, DbContext, compose contracts | Active - 64 tests |
| `HelixERP.Shell.IntegrationTests` | Shell API over real HTTP, YARP forwarding with a fake upstream, upload/download | Active - 13 tests |
| `HelixERP.Architecture.Tests` | Clean Architecture layering, namespace and no-Nginx rules (skips `node_modules`/build output) | Active - 26 tests |
| `src/Frontend/helix-shell` (Vitest) | Session store, auth API client, AuthProvider, login page | Active - 28 tests |
| `HelixERP.Modules.*.UnitTests` | Module domain/application tests | Awaiting module logic |
| `HelixERP.Modules.*.IntegrationTests` | Testcontainers (SQL Server + Redis) tests | Awaiting Testcontainers package |

Run everything:

```powershell
dotnet build HelixERP.sln
dotnet test HelixERP.sln --no-build

# Frontend suite (Vitest + Testing Library)
cd src/Frontend/helix-shell
npm test
```

Current total: **199 passing tests** (171 backend: 132 unit, 13 integration, 26 architecture; plus 28 frontend Vitest tests).

## Test approach

- Unit tests use EF Core InMemory for the shell context and temporary directories for physical
  file storage.
- Integration tests use `WebApplicationFactory<Program>` with an InMemory database so they never
  touch the developer database. They cover login/refresh/logout, module-registry authorization and
  lifecycle, YARP proxying to an in-process fake module, and the file round trip.
- Frontend tests run with Vitest 3 + Testing Library in a jsdom environment (`vite.config.ts` `test` block).
- Testcontainers packages are not available in this environment. SQL Server/Redis container
  integration tests must be added when NuGet access is restored.
- The test factory clears logging providers because Windows EventLog is not writable in the
  restricted sandbox.