namespace App.IntegrationTests.TestSupport;

internal static class DockerAvailability
{
    public static bool IsAvailable()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("BETTERBOOKING_RUN_POSTGRES_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
