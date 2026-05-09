using System.Windows.Controls;
using ClothesSystem.Desktop.ViewModels;

namespace ClothesSystem.Desktop.Views;

public partial class HomePage : Page
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (s, e) => await _viewModel.LoadAsync();
    }
}
