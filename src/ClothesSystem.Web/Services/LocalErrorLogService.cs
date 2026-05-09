using System.Text;

namespace ClothesSystem.Web.Services;

public class LocalErrorLogService : ILocalErrorLogService
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);

    private readonly IWebHostEnvironment _environment;

    public LocalErrorLogService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task WriteAsync(Exception exception, HttpContext? context = null, CancellationToken cancellationToken = default)
    {
        var logsFolder = Path.Combine(_environment.ContentRootPath, "logs");
        Directory.CreateDirectory(logsFolder);

        var logFile = Path.Combine(logsFolder, $"errors-{DateTime.Now:yyyyMMdd}.log");
        var builder = new StringBuilder()
            .AppendLine("============================================================")
            .AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .AppendLine($"RequestId: {context?.TraceIdentifier ?? "N/A"}")
            .AppendLine($"Path: {context?.Request?.Path.Value ?? "N/A"}")
            .AppendLine($"Method: {context?.Request?.Method ?? "N/A"}")
            .AppendLine($"Message: {exception.Message}")
            .AppendLine("StackTrace:")
            .AppendLine(exception.ToString())
            .AppendLine();

        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(logFile, builder.ToString(), Encoding.UTF8, cancellationToken);
        }
        finally
        {
            WriteLock.Release();
        }
    }
}
