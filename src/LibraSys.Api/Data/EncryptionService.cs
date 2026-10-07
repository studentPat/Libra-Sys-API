using System.Security.Cryptography;
using System.Text;
using LibraSys.Api.Configuration;
using Microsoft.Extensions.Options;

namespace LibraSys.Api.Data;

/// <summary>
/// Provides AES-256-CBC encryption and decryption for sensitive member fields (email,
/// contact info). Each ciphertext is prefixed with a random 16-byte IV so that the
/// same plaintext produces different ciphertext on every call (non-deterministic).
///
/// For uniqueness enforcement (the email UNIQUE constraint), a separate HMAC-SHA-256
/// fingerprint is stored alongside the ciphertext. The key used for HMAC is derived
/// from the same EncryptionKey via a fixed domain label so that the same plaintext
/// always produces the same fingerprint without leaking the key.
///
/// Production requirements:
///   - Supply Database:EncryptionKey through user-secrets or an environment variable.
///   - The key must be at least 32 characters.
///   - Rotate via a planned migration; never commit the real key to source control.
/// </summary>
public sealed class EncryptionService
{
    private readonly byte[] _key;          // 32-byte AES-256 key (derived from config)
    private readonly byte[] _hmacKey;      // 32-byte HMAC key (domain-separated)

    public EncryptionService(IOptions<DatabaseOptions> options)
    {
        var rawKey = options.Value.EncryptionKey;
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            throw new InvalidOperationException(
                "Database:EncryptionKey is not configured. " +
                "Use user-secrets or an environment variable. " +
                "The key must be at least 32 characters.");
        }

        // Derive a stable 32-byte AES key from the raw config value.
        _key = SHA256.HashData(Encoding.UTF8.GetBytes("aes:" + rawKey));

        // Derive a separate 32-byte HMAC key (domain-separated).
        _hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes("hmac:" + rawKey));
    }

    /// <summary>Encrypts <paramref name="plaintext"/> and returns the ciphertext bytes.</summary>
    public byte[]? Encrypt(string? plaintext)
    {
        if (plaintext is null)
        {
            return null;
        }

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = _key;
        aes.GenerateIV(); // fresh random 16-byte IV every time

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            var data = Encoding.UTF8.GetBytes(plaintext);
            cs.Write(data, 0, data.Length);
        }

        return ms.ToArray();
    }

    /// <summary>Decrypts ciphertext produced by <see cref="Encrypt"/> and returns the original string.</summary>
    public string? Decrypt(byte[]? ciphertext)
    {
        if (ciphertext is null || ciphertext.Length <= 16)
        {
            return null;
        }

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = _key;

        var iv = ciphertext[..16];
        var data = ciphertext[16..];
        aes.IV = iv;

        using var ms = new MemoryStream(data);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Returns a 32-byte HMAC-SHA-256 fingerprint of <paramref name="plaintext"/> (lower-cased).
    /// Used to enforce UNIQUE constraints on encrypted columns without storing the plaintext.
    /// </summary>
    public byte[]? Fingerprint(string? plaintext)
    {
        if (plaintext is null)
        {
            return null;
        }

        var data = Encoding.UTF8.GetBytes(plaintext.Trim().ToLowerInvariant());
        return HMACSHA256.HashData(_hmacKey, data);
    }
}
