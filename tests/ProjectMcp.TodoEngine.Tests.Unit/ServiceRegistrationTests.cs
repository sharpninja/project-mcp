using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectMCP.TodoEngine;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Repositories;
using ProjectMCP.TodoEngine.Services;
using ProjectMCP.TodoEngine.Views;

namespace ProjectMCP.TodoEngine.Tests.Unit;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddTodoEngine_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        services.AddTodoEngine();

        AssertScoped<ISlugService, SlugService>(services);
        AssertScoped<IProjectRepository, ProjectRepository>(services);
        AssertScoped<IWorkItemRepository, WorkItemRepository>(services);
        AssertScoped<IMilestoneRepository, MilestoneRepository>(services);
        AssertScoped<IReleaseRepository, ReleaseRepository>(services);
        AssertScoped<IResourceRepository, ResourceRepository>(services);
        AssertScoped<IRequirementRepository, RequirementRepository>(services);
        AssertScoped<IStandardRepository, StandardRepository>(services);
        AssertScoped<IIssueRepository, IssueRepository>(services);
        AssertScoped<IKeywordRepository, KeywordRepository>(services);
        AssertScoped<IDomainRepository, DomainRepository>(services);
        AssertScoped<ISystemRepository, SystemRepository>(services);
        AssertScoped<IAssetRepository, AssetRepository>(services);
        AssertScoped<ITodoView, TodoView>(services);
    }

    [Fact]
    public void AddTodoEngine_DoesNotRegisterDbContextWithoutOptions()
    {
        var services = new ServiceCollection();

        services.AddTodoEngine();

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(TodoEngineDbContext));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(DbContextOptions<TodoEngineDbContext>));
    }

    [Fact]
    public void AddTodoEngine_RegistersDbContextWhenConfigured()
    {
        var services = new ServiceCollection();

        services.AddTodoEngine(options =>
        {
            options.Provider = TodoEngineProvider.Sqlite;
            options.ConnectionString = "DataSource=:memory:";
        });

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(TodoEngineDbContext));
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TService));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
    }
}
