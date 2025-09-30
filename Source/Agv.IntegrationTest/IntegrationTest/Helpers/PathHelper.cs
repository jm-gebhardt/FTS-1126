namespace Gebhardt.Acc.IntegrationTest.Helpers;

public static class PathHelper
{
    public static string ToDockerPath(string path)
    {
        while (true)
        {
            if (Path.IsPathRooted(path) && path.Length > 1 && path[1] == ':')
            {
                var drive = char.ToLowerInvariant(path[0]);
                var rest = path[2..].Replace('\\', '/');
                return $"/mnt/{drive}{rest}";
            }

            if (path.StartsWith("/"))
            {
                return path;
            }

            var abs = Path.GetFullPath(path);
            path = abs;
        }
    }
}