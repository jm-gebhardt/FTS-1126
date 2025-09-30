namespace Gebhardt.Acc.IntegrationTest.Helpers;

public static class EnvHelper
{
    public static string GetRequiredEnvVar(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Environment variable {variableName} must be set.");
        }

        return value;
    }
}