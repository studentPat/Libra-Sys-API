USE librasys;

-- Development-only demonstration. Do not put real keys in source control.
SET @demo_key = UNHEX(SHA2('your-secret-key-at-least-32-chars', 256));
SET @member_id = (SELECT member_id FROM members ORDER BY member_id LIMIT 1);

UPDATE members
SET contact_info_ciphertext = AES_ENCRYPT('Demo contact; replace locally', @demo_key)
WHERE member_id = @member_id;

SELECT member_id, contact_info_ciphertext,
       CAST(AES_DECRYPT(contact_info_ciphertext, @demo_key) AS CHAR) AS decrypted_value
FROM members
WHERE member_id = @member_id;

-- Production requirements:
-- * Generate password hashes in ASP.NET 8; never encrypt or store passwords.
-- * Supply encryption keys through a secret manager/environment configuration.
-- * Use TLS for the API-to-MySQL connection.
-- * Encrypt backups independently of column ciphertext.
-- * Rotate keys through a planned migration; do not commit the demo key.
