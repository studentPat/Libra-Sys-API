# LibraSys

LibraSys is a web-based library management system for a single library branch. It provides a searchable physical-book catalog, member accounts, borrowing and returning, reservations, fines, payments, librarian operations, reports, and audit records.

The project combines an ASP.NET Core Web API with a same-origin browser application and a MySQL 8.x database. It is also a course-aligned database project demonstrating transactions, row locking, encryption, least-privilege database access, and query optimization.

## Why LibraSys?

Small libraries need more than a book list: they need reliable workflows for tracking physical copies, enforcing member ownership, preventing duplicate loans, managing reservation queues, and recording financial activity. LibraSys models those workflows with role-protected API endpoints and database constraints designed to preserve consistency under concurrent operations.

## Features

### Public and Member workflows

- Search the catalog by title or ISBN and view book details.
- Register a Member account and sign in with a JWT-backed session.
- Update a member profile.
- Borrow and return physical copies.
- Create, view, and cancel reservations.
- View borrowing history, fines, and payments.
- Track failed logins and temporarily lock an account after five failures.

### Librarian workflows

- Create and update books.
- Add and retire physical copies.
- Search members and update member profiles or statuses.
- Review member borrowings, fines, and payments.
- Create fines and record payments.
- Manage reservation states: ready, fulfilled, expired, or expire-due.
- View overdue, monthly borrowing, member balance, catalog availability, and audit-log reports.

### Database and security demonstrations

- InnoDB transactions for multi-step borrowing, return, reservation, and payment operations.
- Row-locking and concurrency demonstrations.
- ASP.NET password hashing; plaintext passwords are not stored.
- Encrypted member contact information using a configured key.
- Application role policies for `Member`, `Librarian`, and `DBA`.
- MySQL roles, privileges, reporting views, and restricted application access.
- Unique constraints and database checks for active loans, reservations, payments, and catalog identifiers.
- Query-plan and index optimization examples.

## Technology stack

| Area | Technology |
| --- | --- |
| Backend | ASP.NET Core 8 Web API, C# |
| Data access | Dapper, MySqlConnector |
| Authentication | ASP.NET Identity password hashing, JWT Bearer authentication |
| API documentation | Swashbuckle / Swagger UI |
| Frontend | Vanilla HTML, CSS, and JavaScript served by ASP.NET Core |
| Database | MySQL 8.x with InnoDB |
| Testing | xUnit and Microsoft.NET.Test.Sdk |

## How it works

The API and browser client run from the same ASP.NET Core application:

```text
Browser
  ├── /                  Vanilla HTML/CSS/JavaScript client
  └── /swagger           Development API reference
          │
          ▼
ASP.NET Core API
  ├── Public catalog and health endpoints
  ├── JWT authentication and role policies
  ├── Member workflows
  └── Librarian workflows and reports
          │
          ▼
MySQL 8.x / InnoDB
  ├── Catalog, members, loans, reservations
  ├── Fines, payments, and audit logs
  └── Security, transaction, encryption, and optimization scripts
```

Member endpoints derive the member identity from the authenticated JWT rather than accepting an arbitrary member ID. Mutating workflows use explicit database transactions and row locks where needed; database constraints provide additional protection against duplicate active loans, duplicate reservations, and overpayments.

## Project structure

```text
.
├── database/                 MySQL schema, seed data, security, and demonstrations
│   ├── 01_schema.sql         Tables, constraints, triggers, and views
│   ├── 02_seed.sql           Development seed data placeholders
│   ├── 03_security.sql       MySQL roles, users, and privileges
│   ├── 04_transactions.sql   Transaction and concurrency demonstrations
│   ├── 05_encryption.sql     Encryption demonstration
│   ├── 06_query_optimization.sql
│   ├── 07_reservation_constraint_fix.sql
│   └── ERD.md                Mermaid entity-relationship diagram
├── src/LibraSys.Api/         ASP.NET Core API and browser client
│   ├── Features/Auth/        Registration, login, and authenticated identity
│   ├── Features/Catalog/     Public catalog search and details
│   ├── Features/Members/     Member profile and self-service workflows
│   ├── Features/Librarian/   Staff operations and reports
│   └── wwwroot/              Vanilla frontend
├── tests/                    Authentication unit tests
├── TESTING.md                Full local API and browser acceptance procedure
└── LibraSys-Course-Aligned-Project-Paper.md
                              Scope, design rationale, and database evidence plan
```

## Getting started

### Prerequisites

- .NET 8 SDK
- MySQL 8.x with a local development instance
- A MySQL administrator account for initial schema and security setup

### 1. Prepare MySQL

From the repository root, use a disposable local database. `01_schema.sql` recreates the application tables.

```bash
mysql -u YOUR_DBA_USER -p < database/01_schema.sql
```

Before running the seed, replace the `REPLACE_WITH_ASPNET_HASH_*` placeholders in `database/02_seed.sql` with locally generated ASP.NET password hashes. Do not commit local hashes or demo credentials.

```bash
mysql -u YOUR_DBA_USER -p librasys < database/02_seed.sql
mysql -u YOUR_DBA_USER -p librasys < database/03_security.sql
```

Review `database/03_security.sql` and substitute local passwords in your working copy. Run it with the DBA account, then use the restricted `librasys_api` account for the API. If an existing database predates the reservation constraint fix, apply the migration once:

```bash
mysql -u YOUR_DBA_USER -p librasys < database/07_reservation_constraint_fix.sql
```

The complete database demonstration workflow is documented in [`database/README.md`](database/README.md).

### 2. Configure local secrets

The API reads its connection string, encryption key, and JWT signing key from configuration. Keep these values outside source control by using .NET user secrets:

```bash
dotnet user-secrets init --project src/LibraSys.Api
dotnet user-secrets set "Database:ConnectionString" \
  "Server=localhost;Port=3306;Database=librasys;User ID=librasys_api;PASSWORD" \
  --project src/LibraSys.Api
dotnet user-secrets set "Database:EncryptionKey" \
  "A_LOCAL_ENCRYPTION_KEY_OF_AT_LEAST_32_CHARACTERS" \
  --project src/LibraSys.Api
dotnet user-secrets set "Jwt:SigningKey" \
  "A_LOCAL_RANDOM_SIGNING_KEY_OF_AT_LEAST_32_CHARACTERS" \
  --project src/LibraSys.Api
```

Replace the placeholder password and keys locally. Never commit connection strings, passwords, encryption keys, JWT signing keys, or generated secrets.

### 3. Restore, test, and run

```bash
dotnet restore tests/LibraSys.Api.Tests/LibraSys.Api.Tests.csproj
dotnet build src/LibraSys.Api/LibraSys.Api.csproj --no-restore
dotnet test tests/LibraSys.Api.Tests/LibraSys.Api.Tests.csproj --no-restore
dotnet run --project src/LibraSys.Api
```

Open the HTTPS address printed by `dotnet run`. The browser application is available at `/`; Swagger is available at `/swagger` when the application runs in Development. Check `GET /api/health` first when troubleshooting configuration.

For the full database setup, manual API checks, browser acceptance pass, and database-course evidence steps, see [`TESTING.md`](TESTING.md).

## API overview

Swagger provides the request and response schemas for every endpoint. The main route groups are:

| Route | Access | Purpose |
| --- | --- | --- |
| `GET /api/health` | Public | Service health check |
| `/api/catalog` | Public | Search catalog and view book details |
| `/api/auth/login` | Public | Authenticate and receive a JWT |
| `/api/auth/register` | Public | Create a standard Member account |
| `/api/auth/me` | Authenticated | Inspect the current identity |
| `/api/member/*` | Member | Profile, borrowing, reservations, fines, and payments |
| `/api/librarian/catalog/*` | Librarian | Manage books and physical copies |
| `/api/librarian/members/*` | Librarian | Manage members and review member history |
| `/api/librarian/reservations/*` | Librarian | Process reservation queues |
| `/api/librarian/reports/*` | Librarian | Operational and audit reports |
| `/api/librarian/fines` and `/api/librarian/payments` | Librarian | Record financial activity |

## Database design

The relational model separates users and application roles from member records, and models books separately from physical copies. Borrowings connect members to copies; reservations connect members to titles; fines and payments preserve the financial history; transaction logs capture important events.

The verified ERD is available in [`database/ERD.md`](database/ERD.md). The SQL foundation includes foreign keys, unique keys, check constraints, generated keys for active records, indexes for common queries, and a trigger that rejects payments exceeding a fine balance.

## Testing

The automated suite currently covers authentication service behavior, including:

- Successful login and failed-login counter reset
- Invalid credentials and failed-login recording
- Locked-account rejection
- Password hashing during Member registration

Run it with:

```bash
dotnet test tests/LibraSys.Api.Tests/LibraSys.Api.Tests.csproj
```

The broader acceptance procedure is manual and is documented in [`TESTING.md`](TESTING.md).

## Current status and limitations

LibraSys is an implemented course project and working local prototype, not a production-ready enterprise library platform. The repository intentionally excludes:

- Multiple library branches
- Digital books or online reading
- Interlibrary loans and acquisitions
- Automated email, SMS, or push notifications
- Online payment gateways and accounting integrations
- Enterprise key-management infrastructure and multi-region disaster recovery

Runtime validation requires a local MySQL server and locally supplied secrets. The seed data also requires local password-hash substitution before seeded accounts can be used for sign-in.

## Additional documentation

- [`TESTING.md`](TESTING.md) — local run and acceptance test guide
- [`database/README.md`](database/README.md) — database setup and demonstration evidence
- [`database/ERD.md`](database/ERD.md) — Mermaid ERD source
- [`src/LibraSys.Api/README.md`](src/LibraSys.Api/README.md) — API route and local configuration reference
- [`LibraSys-Course-Aligned-Project-Paper.md`](LibraSys-Course-Aligned-Project-Paper.md) — design, scope, and course-alignment details

## License

No license file is currently included in the repository. Add a license before distributing the project publicly.
