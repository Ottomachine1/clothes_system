using ClothesSystem.Application.Common.Models;

namespace ClothesSystem.Application.Common.Interfaces;

public interface IUserDirectoryService
{
    Task<IReadOnlyCollection<OwnerOption>> GetOwnerOptionsAsync(CancellationToken cancellationToken = default);

    Task<OwnerOption?> FindOwnerAsync(string userId, CancellationToken cancellationToken = default);
}
