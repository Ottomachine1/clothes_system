using System.Windows.Controls;
using ClothesSystem.Desktop.ViewModels;

namespace ClothesSystem.Desktop.Views;

public partial class ClothingDetailsPage : Page
{
    private readonly ClothingDetailsViewModel _viewModel;

    public ClothingDetailsPage(ClothingDetailsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public async Task LoadDataAsync(Guid id)
    {
        await _viewModel.LoadAsync(id);
    }
}
