using System.Windows;
using System.Windows.Controls;
using HeavenlyLock.ViewModels;

namespace HeavenlyLock.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Password = ((PasswordBox)sender).Password;
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.NewPasswordAfterRecovery = ((PasswordBox)sender).Password;
    }

    private void ConfirmNewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.ConfirmPasswordAfterRecovery = ((PasswordBox)sender).Password;
    }

    private void RecoveryLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.IsRecoveryMode = true;
    }

    private void BackLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.IsRecoveryMode = false;
    }
}
