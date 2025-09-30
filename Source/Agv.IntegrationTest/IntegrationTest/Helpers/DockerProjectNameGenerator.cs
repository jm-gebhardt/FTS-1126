namespace Gebhardt.Acc.IntegrationTest.Helpers;

public static class DockerProjectNameGenerator
{
    public static string FromComposeFiles(IEnumerable<string> composeFiles)
    {
        var names = composeFiles
            .Select(Path.GetFileName)
            .Select(f =>
            {
                var name = f!
                    .Replace("docker-compose", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(".yaml", "", StringComparison.OrdinalIgnoreCase)
                    .Replace(".yml", "", StringComparison.OrdinalIgnoreCase)
                    .Trim('.', '-', '_');

                return string.IsNullOrWhiteSpace(name) ? "default" : name;
            });

        var combined = string.Join("-", names).ToLowerInvariant();

        if (combined.Length <= 50)
        {
            return combined;
        }
        
        var hash = Math.Abs(combined.GetHashCode()).ToString("X");
        combined = combined[..30] + "-" + hash;

        return combined;
    }
}