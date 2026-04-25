using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using HeavenlyLock.Models;
using HeavenlyLock.Services;

namespace HeavenlyLock.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;
    private readonly string _lockoutFilePath;

    // Basic login state
    private string _password = string.Empty;
    private string _recoveryPhrase = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isCreatingVault;
    private bool _isRecoveryMode;

    // Recovery phrase display
    private bool _showRecoveryPhrase;
    private string _generatedRecoveryPhrase = string.Empty;
    private string _copyPhraseButtonText = "📋 Copy Phrase";

    // Post-recovery: change password flow
    private bool _isPostRecoveryChangePassword;
    private string _newPasswordAfterRecovery = string.Empty;
    private string _confirmPasswordAfterRecovery = string.Empty;
    private Vault? _recoveredVault;
    private byte[]? _recoveredDek;

    // Lockout
    private bool _isLockedOut;
    private string _lockoutMessage = string.Empty;
    private int _failedAttempts;
    private DateTime? _lockoutUntil;
    private Timer? _lockoutTimer;

    // 10 attempts → 10 hours. For testing: set to 1 minute.
    private const int MAX_ATTEMPTS = 10;
    private static readonly TimeSpan LOCKOUT_DURATION = TimeSpan.FromHours(10);

    public LoginViewModel(VaultService vaultService)
    {
        _vaultService = vaultService;
        _isCreatingVault = !_vaultService.VaultExists();

        _lockoutFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HeavenlyLock", "lockout.json");

        LoadLockoutState();

        if (_isLockedOut)
        {
            UpdateLockoutDisplay();
            StartLockoutCountdown();
        }
        else
        {
            StatusMessage = _isCreatingVault ? "Create your master password" : "Enter your master password";
        }
    }

    // ── Properties ──────────────────────────────────────────────────

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string RecoveryPhrase
    {
        get => _recoveryPhrase;
        set => SetProperty(ref _recoveryPhrase, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsCreatingVault
    {
        get => _isCreatingVault;
        set => SetProperty(ref _isCreatingVault, value);
    }

    public bool ShowRecoveryPhrase
    {
        get => _showRecoveryPhrase;
        set => SetProperty(ref _showRecoveryPhrase, value);
    }

    public string GeneratedRecoveryPhrase
    {
        get => _generatedRecoveryPhrase;
        set => SetProperty(ref _generatedRecoveryPhrase, value);
    }

    public string CopyPhraseButtonText
    {
        get => _copyPhraseButtonText;
        set => SetProperty(ref _copyPhraseButtonText, value);
    }

    public bool IsRecoveryMode
    {
        get => _isRecoveryMode;
        set
        {
            if (SetProperty(ref _isRecoveryMode, value))
            {
                StatusMessage = value ? "Enter your 12-word recovery phrase" : "Enter your master password";
                Password = string.Empty;
                RecoveryPhrase = string.Empty;
            }
        }
    }

    public bool IsPostRecoveryChangePassword
    {
        get => _isPostRecoveryChangePassword;
        set => SetProperty(ref _isPostRecoveryChangePassword, value);
    }

    public string NewPasswordAfterRecovery
    {
        get => _newPasswordAfterRecovery;
        set => SetProperty(ref _newPasswordAfterRecovery, value);
    }

    public string ConfirmPasswordAfterRecovery
    {
        get => _confirmPasswordAfterRecovery;
        set => SetProperty(ref _confirmPasswordAfterRecovery, value);
    }

    public bool IsLockedOut
    {
        get => _isLockedOut;
        set => SetProperty(ref _isLockedOut, value);
    }

    public string LockoutMessage
    {
        get => _lockoutMessage;
        set => SetProperty(ref _lockoutMessage, value);
    }

    // ── Commands ─────────────────────────────────────────────────────

    public ICommand UnlockCommand => new RelayCommand(_ => Unlock());
    public ICommand CreateVaultCommand => new RelayCommand(_ => CreateVault());
    public ICommand RecoveryLoginCommand => new RelayCommand(_ => RecoveryLogin());
    public ICommand ToggleRecoveryModeCommand => new RelayCommand(_ => IsRecoveryMode = !IsRecoveryMode);
    public ICommand ContinueAfterRecoveryCommand => new RelayCommand(_ => ContinueAfterRecovery());
    public ICommand CopyRecoveryPhraseCommand => new RelayCommand(_ => CopyRecoveryPhrase());
    public ICommand SetNewPasswordAfterRecoveryCommand => new RelayCommand(_ => ExecuteSetNewPasswordAfterRecovery());

    public event EventHandler<VaultUnlockedEventArgs>? VaultUnlocked;
    public event EventHandler? RequestContinue;

    // ── Login / Unlock ────────────────────────────────────────────────

    private void Unlock()
    {
        if (_isLockedOut) return;

        try
        {
            var vault = _vaultService.OpenVault(Password);
            var dek = _vaultService.TryGetDek(Password);
            if (dek == null)
            {
                HandleFailedAttempt("Failed to derive encryption key.");
                return;
            }
            ResetFailedAttempts();
            VaultUnlocked?.Invoke(this, new VaultUnlockedEventArgs(vault, dek));
            Password = string.Empty;
        }
        catch (CryptographicException)
        {
            HandleFailedAttempt("Invalid password.");
            Password = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    // ── Vault Creation ────────────────────────────────────────────────

    private void CreateVault()
    {
        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
        {
            StatusMessage = "Password must be at least 8 characters.";
            return;
        }

        try
        {
            GeneratedRecoveryPhrase = _vaultService.CreateVault(Password);
            ShowRecoveryPhrase = true;
            StatusMessage = "Vault created! Save your recovery phrase.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating vault: {ex.Message}";
        }
    }

    private void CopyRecoveryPhrase()
    {
        if (string.IsNullOrEmpty(GeneratedRecoveryPhrase)) return;
        try
        {
            Clipboard.SetText(GeneratedRecoveryPhrase);
            CopyPhraseButtonText = "✅ Copied!";
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                Application.Current?.Dispatcher.Invoke(() => CopyPhraseButtonText = "📋 Copy Phrase");
            });
        }
        catch { }
    }

    private void ContinueAfterRecovery()
    {
        ShowRecoveryPhrase = false;
        CopyPhraseButtonText = "📋 Copy Phrase";

        // If we have a recovered vault+DEK, unlock into the dashboard
        if (_recoveredVault != null && _recoveredDek != null)
        {
            ResetFailedAttempts();
            // Re-open vault from disk to get fresh metadata after the password reset
            Vault freshVault;
            try
            {
                freshVault = _vaultService.OpenVaultWithDek(_recoveredDek);
            }
            catch
            {
                freshVault = _recoveredVault;
            }

            var dek = _recoveredDek;
            _recoveredDek = null;
            _recoveredVault = null;
            VaultUnlocked?.Invoke(this, new VaultUnlockedEventArgs(freshVault, dek));
        }
        else
        {
            IsCreatingVault = false;
            StatusMessage = "Enter your master password to unlock.";
            RequestContinue?.Invoke(this, EventArgs.Empty);
        }
    }

    // ── Recovery Login → Change Password Flow ─────────────────────────

    private void RecoveryLogin()
    {
        if (string.IsNullOrWhiteSpace(RecoveryPhrase))
        {
            StatusMessage = "Please enter your recovery phrase.";
            return;
        }

        try
        {
            var vault = _vaultService.RecoverVault(RecoveryPhrase);
            var dek = TryGetDekFromRecovery(RecoveryPhrase, vault.Metadata);
            if (dek == null)
            {
                StatusMessage = "Failed to derive encryption key from recovery phrase.";
                return;
            }

            // Store vault+DEK, then prompt user to change password
            _recoveredVault = vault;
            _recoveredDek = dek;

            IsRecoveryMode = false;
            IsPostRecoveryChangePassword = true;
            StatusMessage = "Recovery successful! Please set a new master password.";
            RecoveryPhrase = string.Empty;
        }
        catch (CryptographicException)
        {
            StatusMessage = "Invalid recovery phrase. Try again.";
            RecoveryPhrase = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void ExecuteSetNewPasswordAfterRecovery()
    {
        if (_recoveredDek == null)
        {
            StatusMessage = "Recovery session expired. Please try again.";
            IsPostRecoveryChangePassword = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPasswordAfterRecovery) || NewPasswordAfterRecovery.Length < 8)
        {
            StatusMessage = "New password must be at least 8 characters.";
            return;
        }

        if (NewPasswordAfterRecovery != ConfirmPasswordAfterRecovery)
        {
            StatusMessage = "Passwords do not match. Try again.";
            return;
        }

        try
        {
            // Re-key vault with new password and generate a new recovery phrase
            string newPhrase = _vaultService.ResetMasterPasswordWithDek(_recoveredDek, NewPasswordAfterRecovery);

            GeneratedRecoveryPhrase = newPhrase;
            IsPostRecoveryChangePassword = false;
            ShowRecoveryPhrase = true;
            StatusMessage = "Password changed! Save your NEW recovery phrase below.";

            NewPasswordAfterRecovery = string.Empty;
            ConfirmPasswordAfterRecovery = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to change password: {ex.Message}";
        }
    }

    private byte[]? TryGetDekFromRecovery(string phrase, VaultMetadata metadata)
    {
        try
        {
            var crypto = new CryptoService();
            byte[] kek = crypto.DeriveKey(
                System.Text.Encoding.UTF8.GetBytes(phrase.ToLowerInvariant().Trim()),
                metadata.RecoverySalt,
                metadata.RecoveryArgon2Iterations,
                metadata.RecoveryArgon2MemoryKB,
                metadata.RecoveryArgon2Parallelism);

            try
            {
                byte[] dek = crypto.UnwrapDek(
                    metadata.RecoveryEncryptedDek,
                    metadata.RecoveryDekNonce,
                    metadata.RecoveryDekTag,
                    kek);
                return dek;
            }
            finally
            {
                crypto.SecureClear(kek);
            }
        }
        catch
        {
            return null;
        }
    }

    // ── Lockout Logic ─────────────────────────────────────────────────

    private void HandleFailedAttempt(string baseMessage)
    {
        _failedAttempts++;
        SaveLockoutState();

        int remaining = MAX_ATTEMPTS - _failedAttempts;

        if (_failedAttempts >= MAX_ATTEMPTS)
        {
            _lockoutUntil = DateTime.UtcNow.Add(LOCKOUT_DURATION);
            _isLockedOut = true;
            SaveLockoutState();
            UpdateLockoutDisplay();
            StartLockoutCountdown();
        }
        else
        {
            string plural = remaining == 1 ? "attempt" : "attempts";
            StatusMessage = $"{baseMessage} {remaining} {plural} remaining before lockout.";
        }
    }

    private void ResetFailedAttempts()
    {
        _failedAttempts = 0;
        _lockoutUntil = null;
        SaveLockoutState();
    }

    private void UpdateLockoutDisplay()
    {
        if (_lockoutUntil.HasValue && DateTime.UtcNow < _lockoutUntil.Value)
        {
            var remaining = _lockoutUntil.Value - DateTime.UtcNow;
            IsLockedOut = true;
            LockoutMessage = $"Too many failed attempts.\nLocked for {FormatTimeSpan(remaining)}.";
            StatusMessage = LockoutMessage;
        }
        else
        {
            // Lockout expired
            IsLockedOut = false;
            _failedAttempts = 0;
            _lockoutUntil = null;
            SaveLockoutState();
            StatusMessage = _isCreatingVault ? "Create your master password" : "Enter your master password";
        }
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds:D2}s";
        return $"{(int)ts.TotalSeconds}s";
    }

    private void StartLockoutCountdown()
    {
        _lockoutTimer?.Dispose();
        _lockoutTimer = new Timer(_ =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                UpdateLockoutDisplay();
                if (!_isLockedOut)
                {
                    _lockoutTimer?.Dispose();
                    _lockoutTimer = null;
                }
            });
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    // ── Lockout Persistence ───────────────────────────────────────────

    private void LoadLockoutState()
    {
        try
        {
            if (!File.Exists(_lockoutFilePath)) return;
            var state = JsonSerializer.Deserialize<LockoutState>(File.ReadAllText(_lockoutFilePath));
            if (state == null) return;

            _failedAttempts = state.FailedAttempts;
            _lockoutUntil = state.LockoutUntil;

            if (_lockoutUntil.HasValue && DateTime.UtcNow < _lockoutUntil.Value)
            {
                _isLockedOut = true;
            }
            else if (_lockoutUntil.HasValue)
            {
                // Lockout expired while app was closed
                _failedAttempts = 0;
                _lockoutUntil = null;
                SaveLockoutState();
            }
        }
        catch { }
    }

    private void SaveLockoutState()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_lockoutFilePath)!);
            var state = new LockoutState { FailedAttempts = _failedAttempts, LockoutUntil = _lockoutUntil };
            File.WriteAllText(_lockoutFilePath, JsonSerializer.Serialize(state));
        }
        catch { }
    }

    private class LockoutState
    {
        [JsonPropertyName("failedAttempts")]
        public int FailedAttempts { get; set; }

        [JsonPropertyName("lockoutUntil")]
        public DateTime? LockoutUntil { get; set; }
    }
}

public class VaultUnlockedEventArgs : EventArgs
{
    public Vault Vault { get; }
    public byte[] Dek { get; }

    public VaultUnlockedEventArgs(Vault vault, byte[] dek)
    {
        Vault = vault;
        Dek = dek;
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
