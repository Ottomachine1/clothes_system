using System.Windows.Controls;
using ClothesSystem.Desktop.ViewModels;

namespace ClothesSystem.Desktop.Views;

public partial class AdminPage : Page
{
    private readonly AdminViewModel _viewModel;

    public AdminPage(AdminViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (s, e) => await _viewModel.LoadAsync();
    }
}
