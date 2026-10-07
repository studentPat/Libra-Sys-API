# LibraSys system handoff

**Handoff date:** 2026-10-08  
**Repository:** `studentPat/Libra-Sys-API`  
**Branch at handoff:** `main`

## Canonical project plan

All future implementation and review must remain aligned with:

<copilot-ref kind="file" target-id="LibraSys-Course-Aligned-Project-Paper.md" label="LibraSys-Course-Aligned-Project-Paper.md" />

That document is the authoritative scope and course-alignment reference. It defines the intended MySQL 8/InnoDB database, ASP.NET 8 Web API, vanilla HTML/CSS/JavaScript frontend, and the four required database demonstrations: transactions and recovery, encryption, authorization and privileges, and query optimization.

## Current implementation state

The repository is a database-first course project with a substantial ASP.NET API slice already present.

### Database foundation

The `database` directory contains:

- `01_schema.sql`: creates the `librasys` database and InnoDB schema for roles, permissions, users, members, books, authors, categories, copies, reservations, borrowings, fines, payments, and transaction logs.
- `02_seed.sql`: seeds application roles, permissions, demo users with intentionally unusable password-hash placeholders, sample authors/categories/books, and one copy per book.
- `03_security.sql`: creates MySQL roles/users and grants least-privilege access to the API and reporting views. It includes an isolated grant-option demonstration.
- `04_transactions.sql`: demonstrates atomic borrowing, rollback, returning, row locking, and a two-session concurrency exercise.
- `05_encryption.sql`: demonstrates MySQL AES encryption for a member address using a local-only demo key placeholder and documents production key requirements.
- `06_query_optimization.sql`: provides baseline catalog, full-text, overdue-loan, and reporting-view `EXPLAIN` queries.
- `ERD.md`: database relationship documentation.
- `README.md`: setup sequence and evidence requirements.

The schema includes important integrity protections: unique active-copy borrowing enforcement through a generated column, foreign keys, status enums, positive monetary checks, a no-overpayment trigger, indexes for the stated workloads, and reporting views.

### ASP.NET API

The project targets .NET 8 and uses Dapper, MySqlConnector, JWT bearer authentication, ASP.NET password hashing, and Swagger. Dependency registration and authentication/authorization policies are configured in `src/LibraSys.Api/Program.cs`.

Implemented API areas include:

- Health endpoint.
- Public catalog search and book details.
- Login and authenticated current-user lookup.
- Member profile, borrowing, return, reservation, fine, and payment operations.
- Librarian fine/payment operations.
- Librarian book and physical-copy management.
- Librarian member search, status/profile management, borrowing/fine/payment lookup.
- Librarian overdue, monthly-borrowing, member-balance, catalog-availability, and audit reports.
- Librarian reservation ready, fulfill, expire, expire-due, and list operations.

Member-facing queries are scoped to the authenticated member. Borrow, return, and reservation mutations use explicit MySQL transactions and row locks. Login failures are tracked; five failures lock an account for fifteen minutes, and a successful login resets the counter.

### Tests and documentation

The repository currently contains focused authentication unit tests in `tests/LibraSys.Api.Tests/AuthServiceTests.cs`, covering valid credentials, invalid credentials, locked accounts, failed-attempt recording, and reset behavior.

The root README describes the database-first phase and the API. The vanilla HTML/CSS/JavaScript frontend has been implemented in the `wwwroot` directory. The API README documents endpoint coverage, local user-secrets configuration, and the database account boundary.

## What is complete relative to the plan

- Course-aligned relational schema and ERD foundation.
- Application roles and database privilege separation.
- Seed data shape and password-hash boundary.
- Transaction, encryption, authorization, and query-optimization demonstration scripts.
- JWT authentication and application role policies.
- Public catalog API.
- Member and librarian API workflows, including transactional mutations.
- Reporting and audit-log API surfaces.
- Basic authentication unit-test coverage.
- Secret-handling guidance that avoids committing passwords, JWT keys, encryption keys, or connection strings.
- Vanilla HTML/CSS/JavaScript frontend UI integrated in `wwwroot`.
- Strict database constraints for unique active reservations.

## Known gaps and evidence still required

These items are not yet demonstrated as complete in the repository:

1. **Live database validation:** SQL scripts have not been evidenced against a configured MySQL 8.x instance in this handoff.
2. **Course evidence:** The required before/after execution plans, timings, rows examined, concurrency observations, rollback results, encryption ciphertext evidence, grant/revoke results, and denied-access results still need to be captured.
3. **Password seed setup:** `02_seed.sql` intentionally contains placeholder hashes and requires locally generated ASP.NET hashes.
4. **Production encryption boundary:** `05_encryption.sql` is a teaching demonstration only; key management, rotation, backup encryption, and loss-of-key behavior remain documentation/deployment work.
5. **API test breadth:** Tests currently focus on authentication. Endpoint authorization, ownership, validation, transaction conflicts, duplicate borrowing/reservations, overpayment, and report behavior need integration or repository-level coverage.
6. **Recovery behavior:** The scripts cover rollback and locking demonstrations, but bounded idempotent retry behavior and broader restore/recovery evidence still need to be exercised.
7. **Operational hardening:** Verify configuration validation, production TLS, migration/deployment procedure, error responses, pagination limits, and audit completeness against the paper.

## Recommended continuation order

1. Provision a disposable MySQL 8.x development database and run `01_schema.sql` through `03_security.sql` with local secrets only.
2. Generate local ASP.NET password hashes, seed demo users, configure API user secrets, and verify health, login, catalog, member, and librarian endpoints.
3. Run each demonstration script and record expected versus observed evidence in the project’s submission artifacts without committing secrets.
4. Add automated API/integration tests for authorization ownership, validation, duplicate operations, overpayment, transaction conflicts, and report queries.
5. Perform end-to-end testing of the recently integrated vanilla frontend against the API workflows, ensuring authorization boundaries hold.
6. Recheck every completion criterion in the canonical paper before preparing the course submission.

## Important handoff constraints

- Treat the canonical project paper reference above as the plan; do not expand scope into multiple branches, digital books, payment gateways, notifications, predictive analytics, or enterprise disaster recovery unless explicitly requested.
- Never commit real passwords, JWT signing keys, encryption keys, connection strings, generated secrets, or database dumps.
- Keep application RBAC separate from MySQL account privileges.
- Preserve member ownership checks even when the API account has broad enough database CRUD privileges to execute the query.
- Keep transaction retries bounded and idempotent so retries cannot create duplicate borrowings, fines, payments, or audit events.
- Prefer precise, parameterized queries and paginated result sets.
