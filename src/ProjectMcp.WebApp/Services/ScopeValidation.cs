using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Exceptions;

namespace ProjectMcp.WebApp.Services;

public static class ScopeValidation
{
    public static void EnsureEnterprise(UserScope scope, Guid enterpriseId)
    {
        if (!scope.AllowedEnterpriseIds.Contains(enterpriseId))
        {
            throw new ScopeViolationException("Enterprise is outside the current user scope.");
        }
    }

    public static void EnsureProject(UserScope scope, Guid projectId)
    {
        if (!scope.AllowedProjectIds.Contains(projectId))
        {
            throw new ScopeViolationException("Project is outside the current user scope.");
        }
    }
}
