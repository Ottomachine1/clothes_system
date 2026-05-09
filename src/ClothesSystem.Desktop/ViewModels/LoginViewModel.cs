using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClothesSystem.Desktop.Services;

namespace ClothesSystem.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;
    private readonly Action<bool>? _onLoginComplete;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public LoginViewModel(
        IAuthenticationService authService,
        INavigationService navigationService,
        Action<bool>? onLoginComplete = null)
    {
        _authService = authService;
        _navigationService = navigationService;
        _onLoginComplete = onLoginComplete;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "请输入用户名";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            // For desktop auth, we need password from UI but this is simplified
            // In a real app, you'd have a password field bound here
            var success = await _authService.LoginAsync(Username, ""); // Password handled differently

            if (success)
            {
                _navigationService.NavigateToHome();
                _onLoginComplete?.Invoke(true);
            }
            else
            {
                ErrorMessage = "登录失败，请检查用户名和密码";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"登录出错: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> TryLoginAsync(string username, string password)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var success = await _authService.LoginAsync(username, password);
            if (success)
            {
                _navigationService.NavigateToHome();
                _onLoginComplete?.Invoke(true);
                return true;
            }
            else
            {
                ErrorMessage = "用户名或密码错误";
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"登录出错: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
