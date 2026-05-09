using System.Windows.Controls;
using ClothesSystem.Desktop.ViewModels;

namespace ClothesSystem.Desktop.Views;

public partial class ClothingListPage : Page
{
    private readonly ClothingListViewModel _viewModel;

    public ClothingListPage(ClothingListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (s, e) => await _viewModel.LoadAsync();
    }
}
