using System.Windows;
using HeavenlyLock.Models;
using HeavenlyLock.Services;
using HeavenlyLock.ViewModels;
using HeavenlyLock.Views;

namespace HeavenlyLock;

public partial class MainWindow : Window
{
    private readonly CryptoService _cryptoService;
    private readonly PasswordGenerator _passwordGenerator;
    private readonly VaultService _vaultService;

    public MainWindow()
    {
        InitializeComponent();
        _cryptoService = new CryptoService();
        _passwordGenerator = new PasswordGenerator();
        _vaultService = new VaultService(_cryptoService, _passwordGenerator);

        SetupLoginView();
    }

    private void SetupLoginView()
    {
        var loginVm = new LoginViewModel(_vaultService);
        loginVm.VaultUnlocked += OnVaultUnlocked;
        loginVm.RequestContinue += (_, _) => SetupLoginView();
        LoginViewControl.DataContext = loginVm;
        LoginViewControl.Visibility = Visibility.Visible;
        DashboardViewControl.Visibility = Visibility.Collapsed;
    }

    private void OnVaultUnlocked(object? sender, VaultUnlockedEventArgs e)
    {
        var mainVm = new MainViewModel(e.Vault, e.Dek, _vaultService, _cryptoService);
        mainVm.RequestLock += OnRequestLock;
        mainVm.RequestReset += OnRequestReset;
        DashboardViewControl.DataContext = mainVm;

        LoginViewControl.Visibility = Visibility.Collapsed;
        DashboardViewControl.Visibility = Visibility.Visible;
    }

    private void OnRequestLock(object? sender, EventArgs e)
    {
        DashboardViewControl.Visibility = Visibility.Collapsed;
        DashboardViewControl.DataContext = null;
        SetupLoginView();
    }

    private void OnRequestReset(object? sender, EventArgs e)
    {
        DashboardViewControl.Visibility = Visibility.Collapsed;
        DashboardViewControl.DataContext = null;
        MessageBox.Show("Vault deleted. The application will now restart.", "Vault Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
        SetupLoginView();
    }
}
