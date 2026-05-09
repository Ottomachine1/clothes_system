using System.Windows.Controls;
using ClothesSystem.Desktop.ViewModels;

namespace ClothesSystem.Desktop.Views;

public partial class ClothingEditPage : Page
{
    private readonly ClothingEditViewModel _viewModel;

    public ClothingEditPage(ClothingEditViewModel viewModel)
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
