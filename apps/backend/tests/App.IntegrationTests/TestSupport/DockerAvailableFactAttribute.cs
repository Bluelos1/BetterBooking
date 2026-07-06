namespace App.IntegrationTests.TestSupport;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DockerAvailableFactAttribute : FactAttribute
{
    public DockerAvailableFactAttribute()
    {
        if (!DockerAvailability.IsAvailable())
        {
            Skip = "PostgreSQL container tests are disabled locally. Set BETTERBOOKING_RUN_POSTGRES_TESTS=true with Docker running to execute them.";
        }
    }
}
