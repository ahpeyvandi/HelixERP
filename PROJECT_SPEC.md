# HelixERP
### Odoo-inspired Modular Architecture
**Production-Ready | Clean Architecture | Modern UI | Iranian Market**

> **This is the single source of truth for the entire project.**  
> AI Agents must always read this file first and follow the current phase.  
> You can freely add new requirements at the end under "Future Requirements" section.

---

## 1. Project Goal

Build **HelixERP** — a modular, multi-tenant, production-grade ERP system heavily inspired by **Odoo**, using **Clean Architecture**.

### Key Characteristics
- Extremely modern and premium UI/UX
- Permission system **exactly like Odoo** (Groups + Access Rights + Record Rules)
- Fully Dockerized (SQL Server runs locally under sanctions)
- Each module runs in its own container (can be on same server or different servers)
- Safe updates without losing user data
- Strong test coverage (AI agents must not break the system)
- Ready for Iranian market (Jalali, RTL, Persian, Active Directory)

---

## 2. Critical Rules for All AI Agents

1. **Always** read this file (`PROJECT_SPEC.md`) and `docs/PROJECT_STATUS.md` before starting any work.
2. Never make a change without writing or updating tests.
3. After every meaningful change → run the relevant tests.
4. If tests fail → the task is **not finished**.
5. There is only **one** status file: `docs/PROJECT_STATUS.md`
6. Prefer small, safe, incremental changes.
7. **Do NOT use Nginx**. Use YARP inside Shell.
8. All file and image uploads must be stored using the **File table in SQL Server** + physical files in Docker volume.

---

## 3. Technology Stack

| Layer                  | Technology                                          |
|------------------------|-----------------------------------------------------|
| Backend                | .NET 8 / .NET 9 (C#)                                |
| ORM                    | **Entity Framework Core** (Code First + Migrations) |
| Frontend               | React 18 + TypeScript + Vite                        |
| UI Library             | Tailwind CSS + Shadcn/ui (preferred)                |
| Mobile                 | Capacitor (responsive PWA first)                    |
| Database               | **SQL Server** (local; Docker images unavailable)   |
| Cache / Event Bus      | Redis                                               |
| Auth                   | JWT + Active Directory (LDAP)                       |
| API Gateway            | **YARP** (inside Shell) – No Nginx                  |
| File Storage           | SQL Server `File` table + Docker Volume             |
| Containerization       | Docker + Docker Compose                             |
| Testing                | xUnit + FluentAssertions + Testcontainers + Moq + NetArchTest |

---

## 4. Database Strategy

- We use **SQL Server**.
- Currently SQL Server runs as a **local Windows installation** (Docker SQL Server images are unreachable under sanctions).
- Later we can move SQL Server to another server or managed instance.
- Only the **Connection String** needs to be changed.
- Use **Entity Framework Core Code First** + Migrations.
- Each module can have its own DbContext (or shared with schemas).
- All migrations must be clean and reversible.

### File & Image Storage Strategy
- Create a central `File` (or `Attachment`) table in the database.
- Store metadata in SQL Server (`Id`, `FileName`, `ContentType`, `Size`, `RelatedEntity`, `RelatedId`, `CreatedBy`, `CreatedAt`, ...).
- Store the **physical file** in a Docker named volume (`erp_files`).
- Never store binary files inside the database (only metadata + path).

---

## 5. Solution Structure
HelixERP/
├── src/
│   ├── BuildingBlocks/
│   │   ├── HelixERP.BuildingBlocks.Domain
│   │   ├── HelixERP.BuildingBlocks.Application
│   │   ├── HelixERP.BuildingBlocks.Infrastructure
│   │   └── HelixERP.BuildingBlocks.Shared
│   │
│   ├── Modules/
│   │   ├── Sales/
│   │   │   ├── HelixERP.Modules.Sales.Domain
│   │   │   ├── HelixERP.Modules.Sales.Application
│   │   │   ├── HelixERP.Modules.Sales.Infrastructure
│   │   │   └── HelixERP.Modules.Sales.Api
│   │   ├── Inventory/
│   │   └── Accounting/
│   │
│   ├── Shell/
│   │   ├── HelixERP.Shell.Api                  # Contains YARP Gateway
│   │   ├── HelixERP.Shell.Application
│   │   └── HelixERP.Shell.Infrastructure
│   │
│   └── Frontend/
│       ├── helix-shell/
│       └── packages/
│
├── tests/
│   ├── HelixERP.BuildingBlocks.UnitTests
│   ├── HelixERP.Modules.Sales.UnitTests
│   ├── HelixERP.Modules.Sales.IntegrationTests
│   ├── HelixERP.Modules.Inventory.UnitTests
│   ├── HelixERP.Modules.Inventory.IntegrationTests
│   ├── HelixERP.Modules.Accounting.UnitTests
│   ├── HelixERP.Modules.Accounting.IntegrationTests
│   ├── HelixERP.Shell.UnitTests
│   ├── HelixERP.Shell.IntegrationTests
│   └── HelixERP.Architecture.Tests
│
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   └── modules/
│       ├── shell.Dockerfile
│       ├── sales.Dockerfile
│       ├── inventory.Dockerfile
│       └── accounting.Dockerfile
│
├── volumes/                               # Persistent data
│   ├── files/                             # Physical files & images
│   ├── redis/
│   └── logs/
│
├── docs/
│   └── PROJECT_STATUS.md                  # ← Only one status file
│
└── README.md

---

## 6. Deployment & Container Rules

- **No Nginx**.
- Each module runs on its **own port**.
- Modules can be deployed on the same server or different servers.
- Shell (with YARP) is the single entry point and routes by IP:Port.
- After purchasing a module, admin registers it in Shell using **IP + Port**.

### Volume Strategy (Data Safety)
Named volumes that must **never** be deleted on update:

| Volume Name           | Purpose                         |
|-----------------------|---------------------------------|
| `erp_files`           | Uploaded files & images         |
| `erp_redis_data`      | Redis persistence               |
| `erp_logs`            | Application logs                |

When updating a module → only the Docker image is replaced. All volumes stay intact.

---

## 7. Core Professional ERP Features

- **Multi-Company** support with strict isolation
- **File / Attachment** system (SQL table + volume)
- **Chatter / Activity** log on documents (Odoo-style)
- **Document Sequences** (SO, PO, INV...) with Jalali year support
- **Audit Log**
- **Background Jobs** (Hangfire or Quartz.NET + Redis)
- **Excel Export** + **PDF Printing**
- **In-app + Email Notifications**
- **Company-specific Settings** (logo, fiscal year, holidays, etc.)
- **Holiday Calendar** (national + per company)

---

## 8. Permission System (Exactly like Odoo)

- Groups (with inheritance)
- Access Rights (`Read`, `Write`, `Create`, `Unlink`)
- Record Rules (domain-based + multi-company)
- Central `IPermissionService` (never hard-code permissions)
- Menu & Action visibility controlled by groups

Standard groups:
- Administration / Settings
- Administration / Access Rights
- Sales / User
- Sales / Manager
- Inventory / User
- Inventory / Manager
- Accounting / Accountant
- Accounting / Adviser
- Employee

---

## 9. Modern Design Requirements

- Very modern, clean, premium look (Linear / Notion / modern Odoo level)
- Tailwind CSS + Shadcn/ui
- First-class RTL
- Beautiful Jalali date picker
- Modern data tables
- Soft shadows + excellent typography (Vazirmatn + Inter)
- Smooth animations
- Great empty states & skeletons
- Dark mode recommended
- Fully responsive + Capacitor-ready

---

## 10. Iranian Specific Requirements

- Default culture: `fa-IR` + RTL
- Primary calendar: **Jalali**
- Holiday Calendar
- Full Persian + English i18n
- Active Directory (LDAP) authentication
- Persian digits (optional toggle)

---

## 11. Testing Strategy (Mandatory)

### High-Risk Areas (must have strong tests)
- Groups + Access Rights + Record Rules
- Multi-company isolation
- Module registration by IP:Port
- File upload (SQL table + volume)
- Cross-module processes
- Jalali date edge cases
- Sequence generation
- Permission checks on every endpoint

### Test Types
- Unit Tests
- Integration Tests (Testcontainers for SQL Server + Redis)
- Architecture Tests (NetArchTest)
- API Contract Tests
- Permission & Record Rule Tests

---

## 12. PROJECT_STATUS.md Structure

AI Agents must keep this file updated:

```markdown
# HelixERP - Project Status

> Last Updated: YYYY-MM-DD HH:mm
> Current Phase: Phase 0 - Foundation

## Overall Progress
- [ ] Solution + Clean Architecture skeleton
- [ ] BuildingBlocks + Architecture Tests
- [ ] Docker Compose + Named Volumes + local SQL Server
- [ ] Entity Framework Core setup + First Migration
- [ ] Shell (YARP + Auth + Odoo Permission System + Module Registry)
- [ ] File Storage service (SQL table + volume)
- [ ] Sales Module
- [ ] Inventory Module
- [ ] Accounting Module
- [ ] Frontend Shell (Modern + RTL + Jalali)
- [ ] Active Directory (LDAP)
- [ ] Multi-Company
- [ ] Document Sequences
- [ ] Chatter / Activity
- [ ] Background Jobs
- [ ] Holiday Calendar
- [ ] Cross-module integration tests

## Detailed Status

### 1. BuildingBlocks
- [ ] 
- **Tests**: 
- **Last Test Run**: 
- **Next Actions**: 

### 2. Infrastructure & Docker
- [ ] docker-compose with Redis + Volumes (SQL Server is local)
- [ ] EF Core configuration
- **Next Actions**: 

### 3. Shell + Security + Gateway
- [ ] 
- **Tests**: 
- **Last Test Run**: 
- **Next Actions**: 

### 4. File Storage
- [ ] File entity + table
- [ ] Upload/Download service
- **Tests**: 
- **Next Actions**: 

### 5. Sales Module
- [ ] 
- **Tests**: 
- **Last Test Run**: 
- **Next Actions**: 

### 6. Inventory Module
- [ ] 
- **Tests**: 
- **Last Test Run**: 
- **Next Actions**: 

### 7. Accounting Module
- [ ] 
- **Tests**: 
- **Last Test Run**: 
- **Next Actions**: 

### 8. Frontend
- [ ] 
- **Next Actions**: 

## Test Health
- Total Unit Tests: 
- Total Integration Tests: 
- Architecture Tests: 
- Last Full Test Run: 
- Known Flaky Tests: 

## Known Issues
- 

## Architecture Decision Records
- AD001: No Nginx – YARP is used inside Shell
- AD002: Files stored via SQL `File` table + Docker volume `erp_files`
- AD003: SQL Server is a local installation under sanctions (moving later requires only a connection-string change)
- AD004: Entity Framework Core Code First + Migrations
- AD005: Permission system follows Odoo model
- AD006: Project name is HelixERP