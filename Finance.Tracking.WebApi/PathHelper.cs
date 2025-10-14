using System;
using System.IO;

namespace Finance.Tracking.WebApi;

public static class PathHelper
{
    public static string GetPath(string fileName)
    {
        string baseDir = AppContext.BaseDirectory;

        // Detect if running inside bin/Debug or bin/Release
        // Common convention: check for bin folder in path
        string dataFolder = baseDir;
        if (baseDir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        {
            // Likely running from VS: move up to project root
            DirectoryInfo? baseDirInfo = new DirectoryInfo(baseDir);
            dataFolder = Path.GetFullPath(Path.Combine(baseDirInfo.Parent?.Parent?.Parent?.Parent?.FullName ?? throw new Exception("Cannot determine data folder")));

        }
        return Path.Combine(dataFolder, fileName);
    }
}
