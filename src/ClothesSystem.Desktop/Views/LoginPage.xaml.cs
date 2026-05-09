using System.Windows;
using System.Windows.Controls;
using ClothesSystem.Desktop.ViewModels;

namespace ClothesSystem.Desktop.Views;

public partial class LoginPage : Page
{
    private readonly LoginViewModel _viewModel;
    private string _password = string.Empty;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        UsernameTextBox.Focus();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _password = PasswordBox.Password;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var success = await _viewModel.TryLoginAsync(_viewModel.Username, _password);
        if (success)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel mainVm)
            {
                mainVm.RefreshUserInfo();
            }
        }
    }
}
