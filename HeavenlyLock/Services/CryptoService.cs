using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Konscious.Security.Cryptography;
using HeavenlyLock.Models;

namespace HeavenlyLock.Services;

public class CryptoService
{
    private const int KEY_SIZE = 32; // 256 bits
    private const int NONCE_SIZE = 12; // 96 bits for GCM
    private const int TAG_SIZE = 16; // 128 bits for GCM

    // Generate a random 256-bit Data Encryption Key (DEK)
    public byte[] GenerateDek()
    {
        byte[] dek = new byte[KEY_SIZE];
        RandomNumberGenerator.Fill(dek);
        return dek;
    }

    // Layer 1: Argon2id KDF
    public byte[] DeriveKey(byte[] password, byte[] salt, int iterations, int memoryKB, int parallelism)
    {
        var argon2 = new Argon2id(password)
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            MemorySize = memoryKB,
            Iterations = iterations
        };
        return argon2.GetBytes(KEY_SIZE);
    }

    // Wrap DEK with a key encryption key (KEK)
    public (byte[] cipherText, byte[] nonce, byte[] tag) WrapDek(byte[] dek, byte[] kek)
    {
        byte[] nonce = new byte[NONCE_SIZE];
        RandomNumberGenerator.Fill(nonce);

        byte[] cipherText = new byte[dek.Length];
        byte[] tag = new byte[TAG_SIZE];

        using var aes = new AesGcm(kek, TAG_SIZE);
        aes.Encrypt(nonce, dek, cipherText, tag);

        return (cipherText, nonce, tag);
    }

    public byte[] UnwrapDek(byte[] cipherText, byte[] nonce, byte[] tag, byte[] kek)
    {
        byte[] dek = new byte[cipherText.Length];

        using var aes = new AesGcm(kek, TAG_SIZE);
        aes.Decrypt(nonce, cipherText, tag, dek);

        return dek;
    }

    // Layer 2: AES-256-GCM for vault encryption (using DEK)
    public (byte[] cipherText, byte[] nonce, byte[] tag) EncryptVault(Vault vault, byte[] dek)
    {
        byte[] plainText = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(vault));
        byte[] nonce = new byte[NONCE_SIZE];
        RandomNumberGenerator.Fill(nonce);

        byte[] cipherText = new byte[plainText.Length];
        byte[] tag = new byte[TAG_SIZE];

        using var aes = new AesGcm(dek, TAG_SIZE);
        aes.Encrypt(nonce, plainText, cipherText, tag);

        return (cipherText, nonce, tag);
    }

    public Vault DecryptVault(byte[] cipherText, byte[] nonce, byte[] tag, byte[] dek)
    {
        byte[] plainText = new byte[cipherText.Length];

        using var aes = new AesGcm(dek, TAG_SIZE);
        aes.Decrypt(nonce, cipherText, tag, plainText);

        string json = Encoding.UTF8.GetString(plainText);
        return JsonSerializer.Deserialize<Vault>(json) ?? throw new CryptographicException("Failed to deserialize vault.");
    }

    // Layer 3: HKDF-derived subkey + AES-256-GCM for individual passwords
    public (byte[] cipherText, byte[] nonce, byte[] tag) EncryptPassword(string password, byte[] dek, Guid entryId)
    {
        byte[] entryIdBytes = Encoding.UTF8.GetBytes(entryId.ToString());
        byte[] subKey = HKDF.DeriveKey(hashAlgorithmName: HashAlgorithmName.SHA256,
                                       ikm: dek,
                                       outputLength: KEY_SIZE,
                                       salt: entryIdBytes,
                                       info: Encoding.UTF8.GetBytes("heavenly-lock-entry"));

        byte[] plainText = Encoding.UTF8.GetBytes(password);
        byte[] nonce = new byte[NONCE_SIZE];
        RandomNumberGenerator.Fill(nonce);

        byte[] cipherText = new byte[plainText.Length];
        byte[] tag = new byte[TAG_SIZE];

        using var aes = new AesGcm(subKey, TAG_SIZE);
        aes.Encrypt(nonce, plainText, cipherText, tag);

        CryptographicOperations.ZeroMemory(subKey);

        return (cipherText, nonce, tag);
    }

    public string DecryptPassword(byte[] cipherText, byte[] nonce, byte[] tag, byte[] dek, Guid entryId)
    {
        byte[] entryIdBytes = Encoding.UTF8.GetBytes(entryId.ToString());
        byte[] subKey = HKDF.DeriveKey(hashAlgorithmName: HashAlgorithmName.SHA256,
                                       ikm: dek,
                                       outputLength: KEY_SIZE,
                                       salt: entryIdBytes,
                                       info: Encoding.UTF8.GetBytes("heavenly-lock-entry"));

        byte[] plainText = new byte[cipherText.Length];

        using var aes = new AesGcm(subKey, TAG_SIZE);
        aes.Decrypt(nonce, cipherText, tag, plainText);

        CryptographicOperations.ZeroMemory(subKey);

        return Encoding.UTF8.GetString(plainText);
    }

    public byte[] GenerateSalt(int size = 32)
    {
        byte[] salt = new byte[size];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public void SecureClear(byte[] bytes)
    {
        if (bytes == null) return;
        CryptographicOperations.ZeroMemory(bytes);
    }
}
