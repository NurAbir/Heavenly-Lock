using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using HeavenlyLock.Models;
using HeavenlyLock.Services;

namespace HeavenlyLock.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly VaultService _vaultService;
    private readonly CryptoService _cryptoService;
    private Vault _vault;
    private byte[] _dek;
    private ObservableCollection<VaultEntryViewModel> _entries = new();
    private VaultEntryViewModel? _selectedEntry;
    private string _searchText = string.Empty;
    private string _statusMessage = "Ready";
    private bool _showChangePassword;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;

    public MainViewModel(Vault vault, byte[] dek, VaultService vaultService, CryptoService cryptoService)
    {
        _vault = vault;
        _dek = dek;
        _vaultService = vaultService;
        _cryptoService = cryptoService;
        LoadEntries();
    }

    public ObservableCollection<VaultEntryViewModel> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }

    public VaultEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterEntries();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool ShowChangePassword
    {
        get => _showChangePassword;
        set => SetProperty(ref _showChangePassword, value);
    }

    public string CurrentPassword
    {
        get => _currentPassword;
        set => SetProperty(ref _currentPassword, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public ICommand AddEntryCommand => new RelayCommand(_ => AddEntry());
    public ICommand DeleteEntryCommand => new RelayCommand(_ => DeleteEntry(), _ => SelectedEntry != null);
    public ICommand SaveCommand => new RelayCommand(_ => SaveVault());
    public ICommand CopyPasswordCommand => new RelayCommand(_ => CopyPassword(), _ => SelectedEntry != null);
    public ICommand CopyUsernameCommand => new RelayCommand(_ => CopyUsername(), _ => SelectedEntry != null);
    public ICommand LockCommand => new RelayCommand(_ => LockVault());
    public ICommand ToggleChangePasswordCommand => new RelayCommand(_ => ShowChangePassword = !ShowChangePassword);
    public ICommand ChangePasswordCommand => new RelayCommand(_ => ExecuteChangePassword());
    public ICommand DeleteVaultCommand => new RelayCommand(_ => ExecuteDeleteVault());

    public event EventHandler? RequestLock;
    public event EventHandler? RequestReset;

    private void LoadEntries()
    {
        Entries.Clear();
        foreach (var entry in _vault.Entries)
        {
            var vm = new VaultEntryViewModel(entry, _cryptoService, _dek);
            vm.DeleteCommand = new RelayCommand(_ => DeleteEntryViewModel(vm));
            Entries.Add(vm);
        }
    }

    private void FilterEntries()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            LoadEntries();
            return;
        }

        var filtered = _vault.Entries
            .Where(e => e.Service.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        e.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        (e.Tags?.Any(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ?? false))
            .ToList();

        Entries.Clear();
        foreach (var entry in filtered)
        {
            var vm = new VaultEntryViewModel(entry, _cryptoService, _dek);
            vm.DeleteCommand = new RelayCommand(_ => DeleteEntryViewModel(vm));
            Entries.Add(vm);
        }
    }

    private void AddEntry()
    {
        var entry = new VaultEntry
        {
            Service = "New Service",
            Username = "",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        _vault.Entries.Add(entry);
        var vm = new VaultEntryViewModel(entry, _cryptoService, _dek);
        vm.DeleteCommand = new RelayCommand(_ => DeleteEntryViewModel(vm));
        Entries.Add(vm);
        SelectedEntry = vm;
        StatusMessage = "New entry added. Fill in the details and save.";
    }

    private void DeleteEntry()
    {
        if (SelectedEntry == null) return;
        DeleteEntryViewModel(SelectedEntry);
    }

    private void DeleteEntryViewModel(VaultEntryViewModel vm)
    {
        var result = MessageBox.Show(
            $"Delete \"{vm.Service}\" permanently?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            _vault.Entries.Remove(vm.Entry);
            Entries.Remove(vm);
            if (SelectedEntry == vm) SelectedEntry = null;
            SaveVault();
            StatusMessage = $"Entry \"{vm.Service}\" deleted.";
        }
    }

    private void SaveVault()
    {
        try
        {
            _vaultService.SaveVault(_vault, _dek);
            StatusMessage = $"Vault saved. {_vault.Entries.Count} entries.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    private void CopyPassword()
    {
        if (SelectedEntry == null) return;
        try
        {
            string password = SelectedEntry.GetDecryptedPassword();
            Clipboard.SetText(password);
            StatusMessage = "Password copied (auto-clear in 30s).";
            _ = Task.Run(async () =>
            {
                await Task.Delay(30000);
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    try { Clipboard.Clear(); } catch { }
                    StatusMessage = "Clipboard cleared.";
                });
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to copy: {ex.Message}";
        }
    }

    private void CopyUsername()
    {
        if (SelectedEntry == null) return;
        Clipboard.SetText(SelectedEntry.Entry.Username);
        StatusMessage = "Username copied.";
    }

    private void ExecuteChangePassword()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            StatusMessage = "Enter your current password.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
        {
            StatusMessage = "New password must be at least 8 characters.";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            StatusMessage = "New passwords do not match.";
            return;
        }

        try
        {
            _vaultService.ChangeMasterPassword(CurrentPassword, NewPassword);

            // Sync the in-memory vault metadata with what was just written to disk.
            // Without this, any subsequent SaveVault call would overwrite the new
            // salt/encrypted-DEK with the old in-memory values, silently reverting
            // the password change and breaking the next login.
            _vault.Metadata = _vaultService.LoadVaultMetadata();

            StatusMessage = "Master password changed successfully.";
            ShowChangePassword = false;
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        catch (CryptographicException)
        {
            StatusMessage = "Current password is incorrect.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to change password: {ex.Message}";
        }
    }

    private void ExecuteDeleteVault()
    {
        var result = MessageBox.Show(
            "This will PERMANENTLY delete your vault and all stored passwords.\n\nThis action cannot be undone.\n\nAre you sure?",
            "DELETE VAULT FOREVER",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var confirm = MessageBox.Show(
                "Final warning: All your passwords will be lost forever.\n\nClick Yes to confirm.",
                "FINAL CONFIRMATION",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation);

            if (confirm == MessageBoxResult.Yes)
            {
                _vaultService.DeleteVault();
                _cryptoService.SecureClear(_dek);
                RequestReset?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void LockVault()
    {
        SaveVault();
        _cryptoService.SecureClear(_dek);
        RequestLock?.Invoke(this, EventArgs.Empty);
    }
}

public class VaultEntryViewModel : BaseViewModel
{
    private readonly CryptoService _crypto;
    private readonly byte[] _dek;

    public VaultEntry Entry { get; }

    /// <summary>Bound to the Delete button in EntryEditorView.</summary>
    public ICommand? DeleteCommand { get; set; }

    public VaultEntryViewModel(VaultEntry entry, CryptoService crypto, byte[] dek)
    {
        Entry = entry;
        _crypto = crypto;
        _dek = dek;
    }

    public string Service
    {
        get => Entry.Service;
        set { Entry.Service = value; Entry.ModifiedAt = DateTime.UtcNow; OnPropertyChanged(); }
    }

    public string Username
    {
        get => Entry.Username;
        set { Entry.Username = value; Entry.ModifiedAt = DateTime.UtcNow; OnPropertyChanged(); }
    }

    public string? Url
    {
        get => Entry.Url;
        set { Entry.Url = value; Entry.ModifiedAt = DateTime.UtcNow; OnPropertyChanged(); }
    }

    public string? Notes
    {
        get => Entry.Notes;
        set { Entry.Notes = value; Entry.ModifiedAt = DateTime.UtcNow; OnPropertyChanged(); }
    }

    public string TagsString
    {
        get => string.Join(", ", Entry.Tags);
        set
        {
            Entry.Tags = value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new List<string>();
            Entry.ModifiedAt = DateTime.UtcNow;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TagsDisplay));
        }
    }

    public string TagsDisplay => string.Join(", ", Entry.Tags);

    public string Password
    {
        get
        {
            if (Entry.EncryptedPassword == null || Entry.EncryptedPassword.Length == 0) return string.Empty;
            try
            {
                return _crypto.DecryptPassword(Entry.EncryptedPassword, Entry.PasswordNonce,
                    Entry.PasswordTag, _dek, Entry.Id);
            }
            catch
            {
                return "[decryption failed]";
            }
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Entry.EncryptedPassword = Array.Empty<byte>();
                Entry.PasswordNonce = Array.Empty<byte>();
                Entry.PasswordTag = Array.Empty<byte>();
            }
            else
            {
                var (ct, nonce, tag) = _crypto.EncryptPassword(value, _dek, Entry.Id);
                Entry.EncryptedPassword = ct;
                Entry.PasswordNonce = nonce;
                Entry.PasswordTag = tag;
            }
            Entry.ModifiedAt = DateTime.UtcNow;
            OnPropertyChanged();
        }
    }

    public string GetDecryptedPassword() => Password;

    public DateTime ModifiedAt => Entry.ModifiedAt;
}
