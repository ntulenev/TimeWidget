using System.IO;

namespace TimeWidget.Infrastructure.Configuration;

internal static class PathResolver
{
    public static string ResolvePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
        if (Path.IsPathRooted(expandedPath))
        {
            return expandedPath;
        }

        var currentDirectoryPath = Path.GetFullPath(expandedPath, Environment.CurrentDirectory);
        if (File.Exists(currentDirectoryPath) || Directory.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        return Path.GetFullPath(expandedPath, AppContext.BaseDirectory);
    }
}
