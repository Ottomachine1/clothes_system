namespace ClothesSystem.Application.Common.Services;

public interface IStyleNumberGenerator
{
    Task<string> GenerateNextStyleNumberAsync(CancellationToken cancellationToken = default);
}
