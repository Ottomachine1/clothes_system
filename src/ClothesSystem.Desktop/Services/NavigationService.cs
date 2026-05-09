using System.Windows.Controls;
using System.Windows;

namespace ClothesSystem.Desktop.Services;

public interface INavigationService
{
    void NavigateToHome();
    void NavigateToClothingList();
    void NavigateToCreate();
    void NavigateToEdit(Guid id);
    void NavigateToDetails(Guid id);
    void NavigateToAdmin();
    void NavigateToLogin();
}

public class NavigationService : INavigationService
{
    private readonly Func<Frame> _frameFactory;
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(Func<Frame> frameFactory, IServiceProvider serviceProvider)
    {
        _frameFactory = frameFactory;
        _serviceProvider = serviceProvider;
    }

    public void NavigateToHome() => NavigateWithLoad(typeof(Views.HomePage));

    public void NavigateToClothingList() => NavigateWithLoad(typeof(Views.ClothingListPage));

    public void NavigateToCreate() => NavigateWithLoad(typeof(Views.ClothingEditPage), Guid.Empty);

    public void NavigateToEdit(Guid id) => NavigateWithLoad(typeof(Views.ClothingEditPage), id);

    public void NavigateToDetails(Guid id) => NavigateWithLoad(typeof(Views.ClothingDetailsPage), id);

    public void NavigateToAdmin() => NavigateWithLoad(typeof(Views.AdminPage));

    public void NavigateToLogin() => NavigateWithLoad(typeof(Views.LoginPage));

    private void NavigateWithLoad(Type pageType, object? parameter = null)
    {
        var frame = _frameFactory();
        if (frame == null)
        {
            return;
        }

        Page? page = pageType.Name switch
        {
            nameof(Views.HomePage) => _serviceProvider.GetService(pageType) as Views.HomePage,
            nameof(Views.ClothingListPage) => _serviceProvider.GetService(pageType) as Views.ClothingListPage,
            nameof(Views.ClothingDetailsPage) => _serviceProvider.GetService(pageType) as Views.ClothingDetailsPage,
            nameof(Views.ClothingEditPage) => _serviceProvider.GetService(pageType) as Views.ClothingEditPage,
            nameof(Views.AdminPage) => _serviceProvider.GetService(pageType) as Views.AdminPage,
            nameof(Views.LoginPage) => _serviceProvider.GetService(pageType) as Views.LoginPage,
            _ => _serviceProvider.GetService(pageType) as Page
        };

        if (page == null)
        {
            return;
        }

        // Load data based on page type
        if (parameter is Guid id)
        {
            switch (page)
            {
                case Views.ClothingDetailsPage detailsPage:
                    _ = detailsPage.LoadDataAsync(id);
                    break;
                case Views.ClothingEditPage editPage:
                    _ = editPage.LoadDataAsync(id);
                    break;
            }
        }
        else if (page is Views.ClothingEditPage editPage && (Guid?)parameter == Guid.Empty)
        {
            _ = editPage.LoadDataAsync(Guid.Empty);
        }

        if (parameter != null && (Guid?)parameter != Guid.Empty)
        {
            frame.Navigate(page, parameter);
        }
        else
        {
            frame.Navigate(page);
        }
    }
}
