using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Serilog;
using ClothesSystem.Desktop.Services;
using ClothesSystem.Desktop.ViewModels;
using ClothesSystem.Desktop.Views;
using ClothesSystem.Infrastructure;
using ClothesSystem.Infrastructure.Persistence;
using ClothesSystem.Infrastructure.Identity;
using ClothesSystem.Application.Common.Interfaces;
using AppContext = System.Windows.Application;

namespace ClothesSystem.Desktop;

public partial class App : AppContext
{
    private IServiceProvider? _serviceProvider;
    private Frame? _mainFrame;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        SetupExceptionHandling();

        var dataPathProvider = new DesktopDataPathProvider();
        ConfigureLogging(dataPathProvider.LogPath);

        Log.Information("应用程序启动");

        // Ensure database directory exists
        var dbDir = Path.GetDirectoryName(dataPathProvider.DatabasePath);
        if (!string.IsNullOrEmpty(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        var services = new ServiceCollection();
        ConfigureServices(services, dataPathProvider);

        _serviceProvider = services.BuildServiceProvider();

        try
        {
            InitializeDatabaseAsync().Wait();
            Log.Information("数据库初始化完成");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "数据库初始化失败");
            MessageBox.Show($"数据库初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }

        ShowLoginWindow();
    }

    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log.Fatal(ex, "Unhandled domain exception");
            MessageBox.Show($"发生严重错误: {ex?.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error(args.Exception, "Unhandled dispatcher exception");
            MessageBox.Show($"发生错误: {args.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
    }

    private static void ConfigureLogging(string logPath)
    {
        Directory.CreateDirectory(logPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logPath, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void ConfigureServices(IServiceCollection services, DesktopDataPathProvider dataPathProvider)
    {
        // Infrastructure - with explicit database path
        services.AddSingleton<IDataPathProvider>(dataPathProvider);
        services.AddInfrastructure(dataPathProvider.DatabasePath);

        // Identity - using AddIdentityCore for desktop (no UI)
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 5;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = true;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<UserManager<ApplicationUser>>();
        services.AddScoped<RoleManager<IdentityRole>>();

        // Desktop services
        services.AddSingleton<IAuthenticationService, DesktopAuthenticationService>();
        services.AddSingleton<ICurrentUserService, DesktopCurrentUserService>();
        services.AddSingleton<IDesktopImageService, DesktopImageService>();
        services.AddSingleton<IDatabaseMigrationService, DatabaseMigrationService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ClothingListViewModel>();
        services.AddTransient<ClothingDetailsViewModel>();
        services.AddTransient<ClothingEditViewModel>();
        services.AddTransient<AdminViewModel>();

        // Navigation - Frame is set after MainWindow is created
        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginPage>();
        services.AddTransient<HomePage>();
        services.AddTransient<ClothingListPage>();
        services.AddTransient<ClothingDetailsPage>();
        services.AddTransient<ClothingEditPage>();
        services.AddTransient<AdminPage>();

        // NavigationService with lazy Frame resolution
        services.AddTransient<INavigationService>(sp =>
        {
            return new NavigationService(
                () => {
                    var mainWindow = sp.GetRequiredService<MainWindow>();
                    return mainWindow.ContentFrame;
                },
                sp);
        });
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = _serviceProvider!.CreateScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
        await migrationService.EnsureDatabaseReadyAsync();
    }

    private void ShowLoginWindow()
    {
        var mainWindow = _serviceProvider!.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Get the ContentFrame from MainWindow
        var contentFrame = mainWindow.ContentFrame;
        if (contentFrame != null)
        {
            _mainFrame = contentFrame;

            var loginPage = _serviceProvider!.GetRequiredService<LoginPage>();
            _mainFrame.Content = loginPage;
        }
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        Log.Information("应用程序退出");
        Log.CloseAndFlush();
    }
}
