namespace ClothesSystem.Application.Common.Models;

public class CurrentUserInfo
{
    public static readonly CurrentUserInfo Anonymous = new()
    {
        UserId = string.Empty,
        DisplayName = "Anonymous",
        IsAuthenticated = false,
        IsAdmin = false
    };

    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsAuthenticated { get; init; }
    public bool IsAdmin { get; init; }
}
