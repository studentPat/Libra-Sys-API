USE librasys;

-- Development-only demonstration. Do not put real keys in source control.
-- This key MUST match Database:EncryptionKey configured in the API's user-secrets.
-- The key derivation mirrors the C# EncryptionService (SHA256 of domain-prefixed key).
SET @raw_key              = 'your-secret-key-at-least-32-chars';
SET @aes_key              = UNHEX(SHA2(CONCAT('aes:',  @raw_key), 256));
SET @hmac_key             = UNHEX(SHA2(CONCAT('hmac:', @raw_key), 256));

SET @member_id = (SELECT member_id FROM members ORDER BY member_id LIMIT 1);

-- Show the current encrypted state of the demo member's email and contact info.
-- The ciphertext columns store AES-256-CBC ciphertext; the fingerprint is an HMAC
-- used only for the UNIQUE constraint so duplicate email detection works without
-- storing the plaintext in the database.
SELECT member_id,
       HEX(email_ciphertext)        AS email_hex,
       AES_DECRYPT(email_ciphertext, @aes_key) AS email_decrypted,
       HEX(email_fingerprint)       AS email_fingerprint_hex,
       AES_DECRYPT(contact_info_ciphertext, @aes_key) AS contact_info_decrypted
FROM members
WHERE member_id = @member_id;

-- Update demo member with a new encrypted email (demonstrates encrypt-on-update flow).
SET @new_email   = 'updated.demo@example.invalid';
SET @new_contact = 'Updated contact info';
UPDATE members
SET email_ciphertext       = AES_ENCRYPT(@new_email,   @aes_key),
    email_fingerprint      = SHA2(LOWER(TRIM(@new_email)), 256),
    contact_info_ciphertext = AES_ENCRYPT(@new_contact, @aes_key)
WHERE member_id = @member_id;

-- Confirm the update decrypts correctly.
SELECT member_id,
       AES_DECRYPT(email_ciphertext, @aes_key)        AS email_decrypted,
       AES_DECRYPT(contact_info_ciphertext, @aes_key) AS contact_info_decrypted
FROM members
WHERE member_id = @member_id;

-- Production requirements:
-- * The AES key must be supplied via user-secrets or an environment variable; never committed.
-- * The C# EncryptionService prepends a random 16-byte IV per ciphertext (non-deterministic
--   encryption). MySQL's AES_ENCRYPT above uses ECB mode without an IV — it is used here
--   only for the seed/demo scripts. The API always encrypts with the C# service (CBC + IV).
-- * Rotate keys through a planned migration where you decrypt with the old key and
--   re-encrypt with the new key before removing the old key from configuration.
-- * Use TLS for the API-to-MySQL connection in production.
