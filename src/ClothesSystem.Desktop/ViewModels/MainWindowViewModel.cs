using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClothesSystem.Desktop.Services;

namespace ClothesSystem.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private string? _currentUserName;

    [ObservableProperty]
    private string _title = "服装款式管理系统";

    public MainWindowViewModel(INavigationService navigationService, IAuthenticationService authService)
    {
        _navigationService = navigationService;
        _authService = authService;

        RefreshUserInfo();
    }

    public void RefreshUserInfo()
    {
        var user = _authService.CurrentUser;
        IsAdmin = user?.IsAdmin ?? false;
        CurrentUserName = user?.DisplayName;
    }

    [RelayCommand]
    private void NavigateHome()
    {
        _navigationService.NavigateToHome();
    }

    [RelayCommand]
    private void NavigateClothingList()
    {
        _navigationService.NavigateToClothingList();
    }

    [RelayCommand]
    private void NavigateCreate()
    {
        _navigationService.NavigateToCreate();
    }

    [RelayCommand]
    private void NavigateAdmin()
    {
        _navigationService.NavigateToAdmin();
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        _navigationService.NavigateToLogin();
    }
}
