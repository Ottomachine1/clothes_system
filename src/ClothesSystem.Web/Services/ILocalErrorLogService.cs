namespace ClothesSystem.Web.Services;

public interface ILocalErrorLogService
{
    Task WriteAsync(Exception exception, HttpContext? context = null, CancellationToken cancellationToken = default);
}
