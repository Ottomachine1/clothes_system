using CommunityToolkit.Mvvm.ComponentModel;
using ClothesSystem.Application.ClothingItems;
using ClothesSystem.Desktop.Services;

namespace ClothesSystem.Desktop.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;

    [ObservableProperty]
    private ClothingDashboardSummaryDto? _summary;

    [ObservableProperty]
    private bool _isLoading = true;

    public AdminViewModel(IClothingService clothingService)
    {
        _clothingService = clothingService;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            Summary = await _clothingService.GetDashboardSummaryAsync(ct);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
