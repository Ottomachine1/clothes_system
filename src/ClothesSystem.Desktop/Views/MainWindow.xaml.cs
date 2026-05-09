using System.Windows;
using ClothesSystem.Desktop.ViewModels;

namespace ClothesSystem.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void NavigateToLogin()
    {
        // This is handled in App.xaml.cs
    }
}
