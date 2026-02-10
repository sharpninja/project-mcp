using System.Security.Claims;
using ProjectMcp.WebApp.Models;

namespace ProjectMcp.WebApp.Services;

public interface IUserScopeService
{
    Task<UserScope> GetScopeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
