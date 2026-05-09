using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Desktop.Services;

namespace ClothesSystem.Desktop.ViewModels;

public partial class ClothingListViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ClothingSearchRequest _search = new();

    [ObservableProperty]
    private IReadOnlyCollection<ClothingItemListItemDto> _items = Array.Empty<ClothingItemListItemDto>();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAdmin;

    // Search filter properties
    [ObservableProperty]
    private string _styleNumberFilter = string.Empty;

    [ObservableProperty]
    private string _keywordFilter = string.Empty;

    public ClothingListViewModel(IClothingService clothingService, INavigationService navigationService)
    {
        _clothingService = clothingService;
        _navigationService = navigationService;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var request = new ClothingSearchRequest
            {
                StyleNumber = StyleNumberFilter,
                Keyword = KeywordFilter
            };

            var result = await _clothingService.SearchAsync(request, ct);
            Items = result.Items;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private void ViewDetails(Guid id)
    {
        _navigationService.NavigateToDetails(id);
    }

    [RelayCommand]
    private void Edit(Guid id)
    {
        _navigationService.NavigateToEdit(id);
    }

    [RelayCommand]
    private void Create()
    {
        _navigationService.NavigateToCreate();
    }

    [RelayCommand]
    private async Task DeleteAsync(Guid id)
    {
        await _clothingService.DeleteAsync(id);
        await LoadAsync();
    }
}
