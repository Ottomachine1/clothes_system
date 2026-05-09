using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Desktop.Services;

namespace ClothesSystem.Desktop.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private int _visibleItemsCount;

    [ObservableProperty]
    private int _myItemsCount;

    [ObservableProperty]
    private int _activeItemsCount;

    [ObservableProperty]
    private int _completedItemsCount;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _isLoading = true;

    public HomeViewModel(IClothingService clothingService, INavigationService navigationService)
    {
        _clothingService = clothingService;
        _navigationService = navigationService;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var summary = await _clothingService.GetDashboardSummaryAsync(ct);
            VisibleItemsCount = summary.VisibleItems;
            MyItemsCount = summary.MyItems;
            ActiveItemsCount = summary.ActiveItems;
            CompletedItemsCount = summary.CompletedItems;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToClothingList()
    {
        _navigationService.NavigateToClothingList();
    }

    [RelayCommand]
    private void NavigateToCreate()
    {
        _navigationService.NavigateToCreate();
    }
}
