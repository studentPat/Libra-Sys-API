# LibraSys database ERD

This diagram and the notes below are derived from `database/01_schema.sql` and
`database/07_reservation_constraint_fix.sql`. The database is MySQL 8 with
`utf8mb4` / `utf8mb4_0900_ai_ci` defaults and InnoDB tables.

```mermaid
erDiagram
    roles ||--o{ users : "has role"
    roles ||--o{ role_permissions : grants
    permissions ||--o{ role_permissions : contains
    users ||--o| members : "has member profile"
    books ||--o{ book_authors : has
    authors ||--o{ book_authors : writes
    books ||--o{ book_categories : has
    categories ||--o{ book_categories : classifies
    books ||--o{ book_copies : contains
    members ||--o{ reservations : places
    books ||--o{ reservations : requested
    members ||--o{ borrowings : makes
    book_copies ||--o{ borrowings : loaned
    borrowings ||--o{ fines : incurs
    users ||--o{ fines : waives
    fines ||--o{ payments : receives
    users ||--o{ payments : records
    users o|--o{ transaction_logs : performs

    roles {
        INT_UNSIGNED role_id PK "AUTO_INCREMENT"
        VARCHAR_40 role_name UK
        TIMESTAMP created_at
    }
    users {
        BIGINT_UNSIGNED user_id PK "AUTO_INCREMENT"
        INT_UNSIGNED role_id FK
        VARCHAR_80 username UK
        VARCHAR_255 password_hash
        ENUM status "active|locked|disabled"
        SMALLINT_UNSIGNED failed_login_count
        DATETIME locked_until "NULL"
        DATETIME password_changed_at
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }
    permissions {
        INT_UNSIGNED permission_id PK "AUTO_INCREMENT"
        VARCHAR_80 permission_name UK
        TIMESTAMP created_at
    }
    role_permissions {
        INT_UNSIGNED role_id PK,FK
        INT_UNSIGNED permission_id PK,FK
    }
    members {
        BIGINT_UNSIGNED member_id PK "AUTO_INCREMENT"
        BIGINT_UNSIGNED user_id FK,UK
        VARCHAR_80 first_name
        VARCHAR_80 last_name
        VARCHAR_255 email UK
        VARCHAR_100 contact_info "NULL"
        VARBINARY_1024 contact_info_ciphertext "NULL"
        ENUM member_status "active|suspended|closed"
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }
    books {
        BIGINT_UNSIGNED book_id PK "AUTO_INCREMENT"
        VARCHAR_20 isbn UK
        VARCHAR_255 title
        VARCHAR_255 publisher "NULL"
        YEAR publication_year "NULL"
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }
    authors {
        BIGINT_UNSIGNED author_id PK "AUTO_INCREMENT"
        VARCHAR_180 name UK
    }
    book_authors {
        BIGINT_UNSIGNED book_id PK,FK
        BIGINT_UNSIGNED author_id PK,FK
    }
    categories {
        INT_UNSIGNED category_id PK "AUTO_INCREMENT"
        VARCHAR_100 category_name UK
    }
    book_categories {
        BIGINT_UNSIGNED book_id PK,FK
        INT_UNSIGNED category_id PK,FK
    }
    book_copies {
        BIGINT_UNSIGNED copy_id PK "AUTO_INCREMENT"
        BIGINT_UNSIGNED book_id FK
        VARCHAR_40 accession_number UK
        ENUM status "available|borrowed|reserved|lost|damaged|retired"
        ENUM item_condition "new|good|fair|damaged"
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }
    reservations {
        BIGINT_UNSIGNED reservation_id PK "AUTO_INCREMENT"
        BIGINT_UNSIGNED member_id FK
        BIGINT_UNSIGNED book_id FK
        DATETIME reserved_at
        DATETIME expires_at "NULL"
        ENUM status "queued|ready|fulfilled|cancelled|expired"
        DATETIME fulfilled_at "NULL"
        DATETIME cancelled_at "NULL"
        VARCHAR_80 active_reservation_key UK "GENERATED; NULL when not active"
    }
    borrowings {
        BIGINT_UNSIGNED borrowing_id PK "AUTO_INCREMENT"
        BIGINT_UNSIGNED copy_id FK
        BIGINT_UNSIGNED member_id FK
        DATETIME borrow_date
        DATETIME due_date
        DATETIME return_date "NULL"
        ENUM status "active|returned|lost|cancelled"
        ENUM return_condition "new|good|fair|damaged; NULL"
        BIGINT_UNSIGNED active_copy_id UK "GENERATED; NULL unless active"
        TIMESTAMP created_at
        TIMESTAMP updated_at
    }
    fines {
        BIGINT_UNSIGNED fine_id PK "AUTO_INCREMENT"
        BIGINT_UNSIGNED borrowing_id FK
        DECIMAL_10_2 amount
        ENUM reason "overdue|lost|damaged|other"
        ENUM status "unpaid|partially_paid|paid|waived"
        DATETIME created_at
        DATETIME settled_at "NULL"
        DATETIME waived_at "NULL"
        BIGINT_UNSIGNED waived_by FK "NULL"
    }
    payments {
        BIGINT_UNSIGNED payment_id PK "AUTO_INCREMENT"
        BIGINT_UNSIGNED fine_id FK
        DECIMAL_10_2 amount_paid
        DATETIME payment_date
        ENUM payment_method "cash|bank_transfer|other"
        VARCHAR_80 receipt_reference UK
        BIGINT_UNSIGNED recorded_by FK
    }
    transaction_logs {
        BIGINT_UNSIGNED transaction_id PK "AUTO_INCREMENT"
        BIGINT_UNSIGNED user_id FK "NULL"
        VARCHAR_80 action
        VARCHAR_80 entity_type
        BIGINT_UNSIGNED entity_id "NULL"
        DATETIME event_time
        BOOLEAN success
        JSON details "NULL"
    }
```

## Nullability, defaults, and generated values

- Every column is `NOT NULL` unless marked `NULL` above.
- Auto-increment primary keys are generated by MySQL.
- Defaults:
  - Timestamp/date audit fields use `CURRENT_TIMESTAMP`; `users.updated_at`,
    `members.updated_at`, `books.updated_at`, `book_copies.updated_at`,
    `borrowings.updated_at` also update automatically on row changes.
  - `users.status = 'active'`, `users.failed_login_count = 0`,
    `members.member_status = 'active'`, `book_copies.status = 'available'`,
    `book_copies.item_condition = 'good'`, `reservations.status = 'queued'`,
    `borrowings.status = 'active'`, `fines.status = 'unpaid'`, and
    `transaction_logs.success = TRUE`.
- `reservations.active_reservation_key` is a stored generated column:
  `CONCAT(member_id, ':', book_id)` only for `queued` or `ready`
  reservations, otherwise `NULL`. Its unique index allows one active
  reservation per member/book pair while preserving completed history.
- `borrowings.active_copy_id` is a stored generated column containing
  `copy_id` only while the borrowing is `active`, otherwise `NULL`. Its unique
  index allows at most one active borrowing per copy.

## Constraints and indexes

| Table | Constraints and indexes |
| --- | --- |
| `roles` | `role_name` unique |
| `users` | `fk_users_role`; `chk_users_failed_login (failed_login_count >= 0)`; `username` unique |
| `permissions` | `permission_name` unique |
| `role_permissions` | Composite primary key `(role_id, permission_id)`; foreign keys to `roles` and `permissions` |
| `members` | `fk_members_user`; `user_id` unique; `email` unique; `ix_members_name (last_name, first_name)` |
| `books` | `isbn` unique; `ix_books_title (title)`; full-text index `ftx_books_title (title)` |
| `authors` | `uq_authors_name (name)` unique |
| `book_authors` | Composite primary key `(book_id, author_id)`; foreign keys to `books` and `authors` |
| `categories` | `category_name` unique |
| `book_categories` | Composite primary key `(book_id, category_id)`; foreign keys to `books` and `categories` |
| `book_copies` | `fk_book_copies_book`; `accession_number` unique; `ix_book_copies_book_status (book_id, status)` |
| `reservations` | `fk_reservations_member`; `fk_reservations_book`; `uq_active_reservation (active_reservation_key)`; `ix_reservations_queue (book_id, status, reserved_at)` |
| `borrowings` | `fk_borrowings_copy`; `fk_borrowings_member`; `chk_borrowings_dates (due_date >= borrow_date)`; `uq_borrowings_one_active_copy (active_copy_id)`; `ix_borrowings_member_status (member_id, status)`; `ix_borrowings_copy_status (copy_id, status)`; `ix_borrowings_due_status (due_date, status)` |
| `fines` | `fk_fines_borrowing`; `fk_fines_waived_by`; `chk_fines_amount (amount > 0)`; `uq_fines_borrowing_reason (borrowing_id, reason)`; `ix_fines_status_created (status, created_at)` |
| `payments` | `fk_payments_fine`; `fk_payments_recorded_by`; `chk_payments_amount (amount_paid > 0)`; `receipt_reference` unique; `ix_payments_fine_date (fine_id, payment_date)` |
| `transaction_logs` | `fk_transaction_logs_user`; `ix_transaction_logs_user_time (user_id, event_time)`; `ix_transaction_logs_entity_time (entity_type, entity_id, event_time)` |

## Views

- `v_available_catalog`: every book with `book_id`, `isbn`, `title`,
  `publisher`, `publication_year`, and `available_copy_count`; the count is
  based only on copies whose status is `available`.
- `v_member_balances`: every member with `member_id`, `first_name`,
  `last_name`, `assessed_fines`, `paid_fines`, and `balance`. Waived fines are
  excluded; payments are summed per fine before calculating the balance.

## Trigger

- `trg_payments_no_overpayment` (`BEFORE INSERT ON payments`) locks the target
  fine, sums existing payments, and rejects the insert with SQLSTATE `45000`
  when the new payment would exceed the fine amount.
