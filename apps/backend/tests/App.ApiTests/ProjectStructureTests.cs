namespace App.ApiTests;

public sealed class ProjectStructureTests
{
    [Fact]
    public void ApiAssembly_IsLoadable()
    {
        Assert.Equal("App.Api", typeof(Program).Assembly.GetName().Name);
    }
}
