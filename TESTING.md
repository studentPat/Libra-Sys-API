# LibraSys local run and acceptance test

This guide runs the complete library system against a local MySQL 8 instance.
Use a disposable local database: `01_schema.sql` begins by replacing the
`librasys` database.

## 1. Prepare the database

From the repository root, log in with a local MySQL administrator and run the
database files in this order:

```powershell
mysql -u YOUR_DBA_USER -p < database\01_schema.sql
mysql -u YOUR_DBA_USER -p librasys < database\02_seed.sql
```

Before testing sign-in, replace each `REPLACE_WITH_ASPNET_HASH_*` value in
`database/02_seed.sql` with a locally generated ASP.NET password hash. Do not
commit the resulting hashes if they reveal your chosen demo passwords. Run the
seed only after making that local replacement.

Review `database/03_security.sql`, substitute local passwords only in your
working copy, then run it as the DBA:

```powershell
mysql -u YOUR_DBA_USER -p librasys < database\03_security.sql
```

Use the `librasys_api` account created by that script for the API. Do not use
the DBA connection string for normal application execution.

## 2. Configure the API locally

```powershell
dotnet user-secrets init --project src\LibraSys.Api
dotnet user-secrets set "Database:ConnectionString" "Server=localhost;Port=3306;Database=librasys;User ID=librasys_api;Password=YOUR_LOCAL_PASSWORD;" --project src\LibraSys.Api
dotnet user-secrets set "Jwt:SigningKey" "A_LOCAL_RANDOM_SECRET_OF_AT_LEAST_32_CHARACTERS" --project src\LibraSys.Api
```

Restore, build, and test:

```powershell
dotnet restore tests\LibraSys.Api.Tests\LibraSys.Api.Tests.csproj
dotnet build src\LibraSys.Api\LibraSys.Api.csproj --no-restore
dotnet test tests\LibraSys.Api.Tests\LibraSys.Api.Tests.csproj --no-restore
dotnet run --project src\LibraSys.Api
```

Open the displayed URL. Check `GET /api/health` first, then open `/swagger`.

## 3. Browser acceptance pass

1. At `/`, search the catalog and open a book detail page.
2. Sign in as a seeded **Member**. Confirm profile, borrowings, reservations,
   fines, and payments load.
3. From the guest navigation, register a new Member using a unique username and
   email plus a password of at least 12 characters; then sign in with that new
   account. Repeating the same username or email must show a conflict instead
   of creating a duplicate user.
4. Update the member profile and confirm the success message.
5. Reserve a catalog book, confirm it appears under Reservations, then cancel
   it. Attempt a duplicate reservation and verify a conflict is shown.
6. Borrow an available copy ID and verify it appears as active; return it with
   a condition and verify the status changes.
7. Sign in as a seeded **Librarian**. Add a book and physical copy, search the
   member, record a permitted payment, and open each report.
8. Verify a Member cannot open `#/librarian`, and a Librarian cannot call a
   member endpoint with another member's token in Swagger.
9. In Swagger, submit invalid IDs, a duplicate ISBN/accession number, a
   duplicate active borrowing/reservation, and an overpayment. Each should
   return a client error rather than a server error.

## 4. Database-course evidence

Run these separately as the appropriate MySQL identity, recording expected and
observed results without recording passwords or encryption keys:

```powershell
mysql -u YOUR_DBA_USER -p librasys < database\04_transactions.sql
mysql -u YOUR_DBA_USER -p librasys < database\05_encryption.sql
mysql -u YOUR_DBA_USER -p librasys < database\06_query_optimization.sql
```

For `04_transactions.sql`, use the two-session section in two MySQL terminals.
Capture the lock/wait result and the rollback state. For the optimization file,
capture the before/after plan, timing, and rows examined with a representative
dataset. For security, capture grants, a permitted reporting-view query, and
denied base-table or grant attempts. Never commit the screenshots if they show
passwords, connection strings, or encryption keys.
