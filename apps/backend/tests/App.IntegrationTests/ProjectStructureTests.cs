using App.Infrastructure.Persistence;

namespace App.IntegrationTests;

public sealed class ProjectStructureTests
{
    [Fact]
    public void InfrastructureAssembly_IsLoadable()
    {
        Assert.Equal("App.Infrastructure", typeof(ApplicationDbContext).Assembly.GetName().Name);
    }
}
