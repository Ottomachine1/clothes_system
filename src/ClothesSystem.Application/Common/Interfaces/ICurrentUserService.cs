using ClothesSystem.Application.Common.Models;

namespace ClothesSystem.Application.Common.Interfaces;

public interface ICurrentUserService
{
    CurrentUserInfo GetCurrentUser();
}
