namespace FashionHub.Web.Application.Authentication;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }

    int? UserId { get; }

    string? Email { get; }

    bool IsInRole(string role);
}
