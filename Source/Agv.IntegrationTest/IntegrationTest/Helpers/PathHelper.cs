using System.Diagnostics;

namespace Gebhardt.Acc.IntegrationTest.Helpers;

public static class PathHelper
{
    private static string? _wslMountRoot;

    public static string ToDockerPath(string path)
    {
        while (true)
        {
            if (Path.IsPathRooted(path) && path.Length > 1 && path[1] == ':')
            {
                var drive = char.ToLowerInvariant(path[0]);
                var rest = path[2..].Replace('\\', '/');

                var mountRoot = GetWslMountRoot();
                return $"{mountRoot}{drive}{rest}";
            }

            if (path.StartsWith("/"))
            {
                return path;
            }

            var abs = Path.GetFullPath(path);
            path = abs;
        }
    }

    private static string GetWslMountRoot()
    {
        if (_wslMountRoot != null)
        {
            return _wslMountRoot;
        }

        try
        {
            var checkHost = Process.Start(new ProcessStartInfo
            {
                FileName = "wsl",
                Arguments = "test -d /mnt/host/c && echo host || echo mnt",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (checkHost != null)
            {
                var result = checkHost.StandardOutput.ReadToEnd().Trim();
                checkHost.WaitForExit();

                _wslMountRoot = result == "host" ? "/mnt/host/" : "/mnt/";
                return _wslMountRoot;
            }
        }
        catch
        {
            // If WSL command fails, default to traditional
        }

        _wslMountRoot = "/mnt/";
        return _wslMountRoot;
    }
}