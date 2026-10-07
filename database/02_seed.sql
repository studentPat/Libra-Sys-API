USE librasys;

INSERT INTO roles (role_name) VALUES
('Guest'), ('Member'), ('Librarian'), ('DBA');

INSERT INTO permissions (permission_name) VALUES
('catalog.read'), ('profile.own.read'), ('profile.own.update'),
('borrowing.create'), ('borrowing.return'), ('reservation.manage'),
('books.manage'), ('copies.manage'), ('members.manage'),
('fines.manage'), ('payments.manage'), ('reports.read'),
('audit.read'), ('database.security.manage');

INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r CROSS JOIN permissions p
WHERE (r.role_name = 'Guest' AND p.permission_name = 'catalog.read')
   OR (r.role_name = 'Member' AND p.permission_name IN
       ('catalog.read', 'profile.own.read', 'profile.own.update',
        'borrowing.create', 'borrowing.return', 'reservation.manage'))
   OR (r.role_name = 'Librarian' AND p.permission_name IN
       ('catalog.read', 'books.manage', 'copies.manage', 'members.manage',
        'fines.manage', 'payments.manage', 'reports.read', 'audit.read'))
   OR (r.role_name = 'DBA' AND p.permission_name = 'database.security.manage');

-- These are placeholders, not usable credentials. Replace with ASP.NET-generated
-- password hashes during local setup; never store plaintext passwords in SQL.
INSERT INTO users (role_id, username, password_hash)
SELECT role_id, 'demo_librarian', 'AQAAAAIAAYagAAAAENrd8dqunmsoTr+cBD6Nze40Rx3Hq7d4sD/P4cQuP2CdWJ2MnLL4VbJxk8Pk1kpikA=='
FROM roles WHERE role_name = 'Librarian';

INSERT INTO users (role_id, username, password_hash)
SELECT role_id, 'demo_member', 'AQAAAAIAAYagAAAAEKTwOCBTgtNuwx5YI1IDOlDgUIFV0QjxYpeEBcbg2iSK9NRJTBZCJwR1C0hchkTWHw=='
FROM roles WHERE role_name = 'Member';

-- IMPORTANT: Replace LOCAL_DEMO_KEY_REPLACE_ME with your actual Database:EncryptionKey value.
-- This key MUST match the key configured in user-secrets/environment for the API.
-- The AES key is derived as SHA256("aes:" + key) and HMAC key as SHA256("hmac:" + key),
-- which mirrors the C# EncryptionService derivation.
-- Never commit a real key to source control.
SET @raw_key        = 'your-secret-key-at-least-32-chars';
SET @aes_key        = UNHEX(SHA2(CONCAT('aes:', @raw_key), 256));
SET @hmac_key       = UNHEX(SHA2(CONCAT('hmac:', @raw_key), 256));

SET @demo_email     = 'demo.member@example.invalid';
SET @demo_contact   = 'not-a-real-contact';

INSERT INTO members (user_id, first_name, last_name,
                     email_ciphertext, email_fingerprint,
                     contact_info_ciphertext)
SELECT user_id,
       'Demo', 'Member',
       AES_ENCRYPT(@demo_email,   @aes_key),
       UNHEX(SHA2(LOWER(TRIM(@demo_email)), 256)),
       AES_ENCRYPT(@demo_contact, @aes_key)
FROM users WHERE username = 'demo_member';

INSERT INTO authors (name) VALUES
('Robert C. Martin'), ('Martin Fowler'), ('Andrew Hunt');

INSERT INTO categories (category_name) VALUES
('Software Engineering'), ('Reference'), ('Programming');

INSERT INTO books (isbn, title, publisher, publication_year) VALUES
('9780132350884', 'Clean Code', 'Prentice Hall', 2008),
('9780201485677', 'Refactoring', 'Addison-Wesley', 1999),
('9780135957059', 'The Pragmatic Programmer', 'Addison-Wesley', 2019);

INSERT INTO book_authors (book_id, author_id)
SELECT b.book_id, a.author_id FROM books b JOIN authors a ON
    (b.isbn = '9780132350884' AND a.name = 'Robert C. Martin')
 OR (b.isbn = '9780201485677' AND a.name = 'Martin Fowler')
 OR (b.isbn = '9780135957059' AND a.name = 'Andrew Hunt');

INSERT INTO book_categories (book_id, category_id)
SELECT b.book_id, c.category_id FROM books b JOIN categories c ON
    (b.isbn IN ('9780132350884', '9780201485677') AND c.category_name = 'Software Engineering')
 OR (b.isbn = '9780135957059' AND c.category_name IN ('Reference', 'Programming'));

INSERT INTO book_copies (book_id, accession_number, status, item_condition)
SELECT book_id, CONCAT('ACC-', LPAD(book_id, 4, '0'), '-01'), 'available', 'good' FROM books;
