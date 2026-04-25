using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HeavenlyLock.Models;

namespace HeavenlyLock.Services;

public class VaultService
{
    private readonly CryptoService _crypto;
    private readonly PasswordGenerator _generator;
    private readonly string _vaultPath;

    public VaultService(CryptoService crypto, PasswordGenerator generator)
    {
        _crypto = crypto;
        _generator = generator;
        _vaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HeavenlyLock",
            "vault.heavenly");
    }

    public string VaultPath => _vaultPath;

    public bool VaultExists()
    {
        return File.Exists(_vaultPath);
    }

    public string CreateVault(string masterPassword)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_vaultPath)!);

        byte[] dek = _crypto.GenerateDek();

        var salt = _crypto.GenerateSalt();
        var metadata = new VaultMetadata
        {
            Salt = salt,
            Argon2Iterations = 3,
            Argon2MemoryKB = 65536,
            Argon2Parallelism = 4
        };

        byte[] masterKek = _crypto.DeriveKey(
            Encoding.UTF8.GetBytes(masterPassword),
            salt, metadata.Argon2Iterations, metadata.Argon2MemoryKB, metadata.Argon2Parallelism);

        var (encDek, dekNonce, dekTag) = _crypto.WrapDek(dek, masterKek);
        metadata.EncryptedDek = encDek;
        metadata.DekNonce = dekNonce;
        metadata.DekTag = dekTag;

        string recoveryPhrase = _generator.GenerateRecoveryPhrase(12);

        var recoverySalt = _crypto.GenerateSalt();
        metadata.RecoverySalt = recoverySalt;
        metadata.RecoveryArgon2Iterations = 3;
        metadata.RecoveryArgon2MemoryKB = 65536;
        metadata.RecoveryArgon2Parallelism = 4;

        byte[] recoveryKek = _crypto.DeriveKey(
            Encoding.UTF8.GetBytes(recoveryPhrase),
            recoverySalt, metadata.RecoveryArgon2Iterations, metadata.RecoveryArgon2MemoryKB, metadata.RecoveryArgon2Parallelism);

        var (recEncDek, recDekNonce, recDekTag) = _crypto.WrapDek(dek, recoveryKek);
        metadata.RecoveryEncryptedDek = recEncDek;
        metadata.RecoveryDekNonce = recDekNonce;
        metadata.RecoveryDekTag = recDekTag;

        var vault = new Vault { Metadata = metadata };
        var (cipherText, vaultNonce, vaultTag) = _crypto.EncryptVault(vault, dek);

        metadata.VaultNonce = vaultNonce;
        metadata.VaultTag = vaultTag;
        vault.Metadata = metadata;

        var container = new VaultContainer
        {
            Metadata = metadata,
            EncryptedData = cipherText
        };

        File.WriteAllBytes(_vaultPath, JsonSerializer.SerializeToUtf8Bytes(container));

        _crypto.SecureClear(dek);
        _crypto.SecureClear(masterKek);
        _crypto.SecureClear(recoveryKek);

        return recoveryPhrase;
    }

    public Vault OpenVault(string masterPassword)
    {
        if (!VaultExists())
            throw new FileNotFoundException("Vault not found.");

        var container = JsonSerializer.Deserialize<VaultContainer>(File.ReadAllBytes(_vaultPath))
            ?? throw new InvalidDataException("Invalid vault file.");

        ValidateVaultEncryptionParams(container.Metadata);

        byte[] masterKek = _crypto.DeriveKey(
            Encoding.UTF8.GetBytes(masterPassword),
            container.Metadata.Salt,
            container.Metadata.Argon2Iterations,
            container.Metadata.Argon2MemoryKB,
            container.Metadata.Argon2Parallelism);

        byte[] dek;
        try
        {
            dek = _crypto.UnwrapDek(
                container.Metadata.EncryptedDek,
                container.Metadata.DekNonce,
                container.Metadata.DekTag,
                masterKek);
        }
        catch (CryptographicException)
        {
            _crypto.SecureClear(masterKek);
            throw new CryptographicException("Invalid master password.");
        }
        finally
        {
            _crypto.SecureClear(masterKek);
        }

        try
        {
            return _crypto.DecryptVault(
                container.EncryptedData,
                container.Metadata.VaultNonce,
                container.Metadata.VaultTag,
                dek);
        }
        finally
        {
            _crypto.SecureClear(dek);
        }
    }

    /// <summary>Opens vault using a known DEK directly (used after recovery password reset).</summary>
    public Vault OpenVaultWithDek(byte[] dek)
    {
        if (!VaultExists())
            throw new FileNotFoundException("Vault not found.");

        var container = JsonSerializer.Deserialize<VaultContainer>(File.ReadAllBytes(_vaultPath))
            ?? throw new InvalidDataException("Invalid vault file.");

        ValidateVaultEncryptionParams(container.Metadata);

        return _crypto.DecryptVault(
            container.EncryptedData,
            container.Metadata.VaultNonce,
            container.Metadata.VaultTag,
            dek);
    }

    public Vault RecoverVault(string recoveryPhrase)
    {
        if (!VaultExists())
            throw new FileNotFoundException("Vault not found.");

        var container = JsonSerializer.Deserialize<VaultContainer>(File.ReadAllBytes(_vaultPath))
            ?? throw new InvalidDataException("Invalid vault file.");

        ValidateVaultEncryptionParams(container.Metadata);

        byte[] recoveryKek = _crypto.DeriveKey(
            Encoding.UTF8.GetBytes(recoveryPhrase.ToLowerInvariant().Trim()),
            container.Metadata.RecoverySalt,
            container.Metadata.RecoveryArgon2Iterations,
            container.Metadata.RecoveryArgon2MemoryKB,
            container.Metadata.RecoveryArgon2Parallelism);

        byte[] dek;
        try
        {
            dek = _crypto.UnwrapDek(
                container.Metadata.RecoveryEncryptedDek,
                container.Metadata.RecoveryDekNonce,
                container.Metadata.RecoveryDekTag,
                recoveryKek);
        }
        catch (CryptographicException)
        {
            _crypto.SecureClear(recoveryKek);
            throw new CryptographicException("Invalid recovery phrase.");
        }
        finally
        {
            _crypto.SecureClear(recoveryKek);
        }

        try
        {
            return _crypto.DecryptVault(
                container.EncryptedData,
                container.Metadata.VaultNonce,
                container.Metadata.VaultTag,
                dek);
        }
        finally
        {
            _crypto.SecureClear(dek);
        }
    }

    /// <summary>
    /// Resets the master password using the raw DEK (from recovery).
    /// Also generates a brand-new recovery phrase. Returns the new recovery phrase.
    /// </summary>
    public string ResetMasterPasswordWithDek(byte[] dek, string newPassword)
    {
        if (!VaultExists())
            throw new FileNotFoundException("Vault not found.");

        var container = JsonSerializer.Deserialize<VaultContainer>(File.ReadAllBytes(_vaultPath))
            ?? throw new InvalidDataException("Invalid vault file.");

        // New master password KDF
        var newSalt = _crypto.GenerateSalt();
        container.Metadata.Salt = newSalt;

        byte[] newKek = _crypto.DeriveKey(
            Encoding.UTF8.GetBytes(newPassword),
            newSalt,
            container.Metadata.Argon2Iterations,
            container.Metadata.Argon2MemoryKB,
            container.Metadata.Argon2Parallelism);

        var (newEncDek, newDekNonce, newDekTag) = _crypto.WrapDek(dek, newKek);
        container.Metadata.EncryptedDek = newEncDek;
        container.Metadata.DekNonce = newDekNonce;
        container.Metadata.DekTag = newDekTag;
        _crypto.SecureClear(newKek);

        // Generate a brand-new recovery phrase
        string newRecoveryPhrase = _generator.GenerateRecoveryPhrase(12);
        var newRecoverySalt = _crypto.GenerateSalt();
        container.Metadata.RecoverySalt = newRecoverySalt;

        byte[] newRecoveryKek = _crypto.DeriveKey(
            Encoding.UTF8.GetBytes(newRecoveryPhrase),
            newRecoverySalt,
            container.Metadata.RecoveryArgon2Iterations,
            container.Metadata.RecoveryArgon2MemoryKB,
            container.Metadata.RecoveryArgon2Parallelism);

        var (recEncDek, recDekNonce, recDekTag) = _crypto.WrapDek(dek, newRecoveryKek);
        container.Metadata.RecoveryEncryptedDek = recEncDek;
        container.Metadata.RecoveryDekNonce = recDekNonce;
        container.Metadata.RecoveryDekTag = recDekTag;
        _crypto.SecureClear(newRecoveryKek);

        // Re-encrypt vault with updated metadata
        var vault = _crypto.DecryptVault(
            container.EncryptedData,
            container.Metadata.VaultNonce,
            container.Metadata.VaultTag,
            dek);

        vault.Metadata = container.Metadata;

        var (cipherText, vaultNonce, vaultTag) = _crypto.EncryptVault(vault, dek);
        container.Metadata.VaultNonce = vaultNonce;
        container.Metadata.VaultTag = vaultTag;
        container.EncryptedData = cipherText;

        File.WriteAllBytes(_vaultPath, JsonSerializer.SerializeToUtf8Bytes(container));

        return newRecoveryPhrase;
    }

    public void ChangeMasterPassword(string oldPassword, string newPassword)
    {
        if (!VaultExists())
            throw new FileNotFoundException("Vault not found.");

        var container = JsonSerializer.Deserialize<VaultContainer>(File.ReadAllBytes(_vaultPath))
            ?? throw new InvalidDataException("Invalid vault file.");

        ValidateVaultEncryptionParams(container.Metadata);

        byte[] oldKek = _crypto.DeriveKey(
            Encoding.UTF8.GetBytes(oldPassword),
            container.Metadata.Salt,
            container.Metadata.Argon2Iterations,
            container.Metadata.Argon2MemoryKB,
            container.Metadata.Argon2Parallelism);

        byte[] dek;
        try
        {
            dek = _crypto.UnwrapDek(
                container.Metadata.EncryptedDek,
                container.Metadata.DekNonce,
                container.Metadata.DekTag,
                oldKek);
        }
        catch (CryptographicException)
        {
            _crypto.SecureClear(oldKek);
            throw new CryptographicException("Invalid current password.");
        }
        finally
        {
            _crypto.SecureClear(oldKek);
        }

        try
        {
            var newSalt = _crypto.GenerateSalt();
            container.Metadata.Salt = newSalt;

            byte[] newKek = _crypto.DeriveKey(
                Encoding.UTF8.GetBytes(newPassword),
                newSalt,
                container.Metadata.Argon2Iterations,
                container.Metadata.Argon2MemoryKB,
                container.Metadata.Argon2Parallelism);

            var (newEncDek, newDekNonce, newDekTag) = _crypto.WrapDek(dek, newKek);
            container.Metadata.EncryptedDek = newEncDek;
            container.Metadata.DekNonce = newDekNonce;
            container.Metadata.DekTag = newDekTag;
            _crypto.SecureClear(newKek);

            var vault = _crypto.DecryptVault(
                container.EncryptedData,
                container.Metadata.VaultNonce,
                container.Metadata.VaultTag,
                dek);

            vault.Metadata = container.Metadata;

            var (cipherText, vaultNonce, vaultTag) = _crypto.EncryptVault(vault, dek);
            container.Metadata.VaultNonce = vaultNonce;
            container.Metadata.VaultTag = vaultTag;
            container.EncryptedData = cipherText;

            File.WriteAllBytes(_vaultPath, JsonSerializer.SerializeToUtf8Bytes(container));
        }
        finally
        {
            _crypto.SecureClear(dek);
        }
    }

    public void SaveVault(Vault vault, byte[] dek)
    {
        var container = JsonSerializer.Deserialize<VaultContainer>(File.ReadAllBytes(_vaultPath))
            ?? throw new InvalidDataException("Invalid vault file.");

        var (cipherText, nonce, tag) = _crypto.EncryptVault(vault, dek);

        vault.Metadata.VaultNonce = nonce;
        vault.Metadata.VaultTag = tag;

        var newContainer = new VaultContainer
        {
            Metadata = vault.Metadata,
            EncryptedData = cipherText
        };

        File.WriteAllBytes(_vaultPath, JsonSerializer.SerializeToUtf8Bytes(newContainer));
    }

    public byte[]? TryGetDek(string password)
    {
        if (!VaultExists()) return null;

        try
        {
            var container = JsonSerializer.Deserialize<VaultContainer>(File.ReadAllBytes(_vaultPath));
            if (container == null) return null;

            ValidateVaultEncryptionParams(container.Metadata);

            byte[] kek = _crypto.DeriveKey(
                Encoding.UTF8.GetBytes(password),
                container.Metadata.Salt,
                container.Metadata.Argon2Iterations,
                container.Metadata.Argon2MemoryKB,
                container.Metadata.Argon2Parallelism);

            try
            {
                byte[] dek = _crypto.UnwrapDek(
                    container.Metadata.EncryptedDek,
                    container.Metadata.DekNonce,
                    container.Metadata.DekTag,
                    kek);
                return dek;
            }
            catch
            {
                _crypto.SecureClear(kek);
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private void ValidateVaultEncryptionParams(VaultMetadata metadata)
    {
        if (metadata.VaultNonce == null || metadata.VaultNonce.Length != 12)
            throw new CryptographicException("Invalid vault nonce. Vault file may be corrupted.");
        if (metadata.VaultTag == null || metadata.VaultTag.Length != 16)
            throw new CryptographicException("Invalid vault tag. Vault file may be corrupted.");
    }

    /// <summary>Reads the vault metadata from disk (used to sync in-memory state after a password change).</summary>
    public VaultMetadata LoadVaultMetadata()
    {
        var container = JsonSerializer.Deserialize<VaultContainer>(File.ReadAllBytes(_vaultPath))
            ?? throw new InvalidDataException("Invalid vault file.");
        return container.Metadata;
    }

    public void DeleteVault()
    {
        if (VaultExists())
        {
            File.Delete(_vaultPath);
        }
    }

    private class VaultContainer
    {
        [JsonPropertyName("metadata")]
        public VaultMetadata Metadata { get; set; } = new();

        [JsonPropertyName("data")]
        public byte[] EncryptedData { get; set; } = Array.Empty<byte>();
    }
}
