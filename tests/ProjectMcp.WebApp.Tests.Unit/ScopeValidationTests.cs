using ProjectMcp.WebApp.Models;
using ProjectMcp.WebApp.Services;
using ProjectMCP.TodoEngine.Exceptions;

namespace ProjectMcp.WebApp.Tests.Unit;

public sealed class ScopeValidationTests
{
    [Fact]
    public void EnsureEnterprise_ThrowsWhenNotAllowed()
    {
        var scope = new UserScope(Array.Empty<Guid>(), Array.Empty<Guid>(), null);

        Assert.Throws<ScopeViolationException>(() => ScopeValidation.EnsureEnterprise(scope, Guid.NewGuid()));
    }

    [Fact]
    public void EnsureEnterprise_DoesNotThrowWhenAllowed()
    {
        var enterpriseId = Guid.NewGuid();
        var scope = new UserScope(new[] { enterpriseId }, Array.Empty<Guid>(), null);

        ScopeValidation.EnsureEnterprise(scope, enterpriseId);
    }

    [Fact]
    public void EnsureProject_ThrowsWhenNotAllowed()
    {
        var scope = new UserScope(Array.Empty<Guid>(), Array.Empty<Guid>(), null);

        Assert.Throws<ScopeViolationException>(() => ScopeValidation.EnsureProject(scope, Guid.NewGuid()));
    }

    [Fact]
    public void EnsureProject_DoesNotThrowWhenAllowed()
    {
        var projectId = Guid.NewGuid();
        var scope = new UserScope(Array.Empty<Guid>(), new[] { projectId }, null);

        ScopeValidation.EnsureProject(scope, projectId);
    }
}
