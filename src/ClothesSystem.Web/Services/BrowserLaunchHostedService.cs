using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ClothesSystem.Web.Services;

public class BrowserLaunchHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IServer _server;
    private readonly ILogger<BrowserLaunchHostedService> _logger;
    private readonly bool _openBrowserOnStart;

    public BrowserLaunchHostedService(
        IHostApplicationLifetime applicationLifetime,
        IServer server,
        IConfiguration configuration,
        ILogger<BrowserLaunchHostedService> logger)
    {
        _applicationLifetime = applicationLifetime;
        _server = server;
        _logger = logger;
        _openBrowserOnStart = configuration.GetValue("OpenBrowserOnStart", true);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_openBrowserOnStart)
        {
            _applicationLifetime.ApplicationStarted.Register(OpenBrowser);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OpenBrowser()
    {
        try
        {
            var address = _server.Features.Get<IServerAddressesFeature>()?
                .Addresses
                .FirstOrDefault(address => address.StartsWith("http://", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            var browserUrl = address
                .Replace("0.0.0.0", "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                .Replace("[::]", "127.0.0.1", StringComparison.OrdinalIgnoreCase);

            Process.Start(new ProcessStartInfo(browserUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to launch the default browser.");
        }
    }
}
