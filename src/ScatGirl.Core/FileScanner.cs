namespace ScatGirl.Core;

static class FileScanner
{
    static readonly HashSet<string> SkippedDirs =
        ["bin", "obj", ".vs", ".git", ".github", "node_modules", ".nuke"];

    internal static IEnumerable<string> GetCSharpFiles(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return [];

        return Enumerate(rootPath);
    }

    static IEnumerable<string> Enumerate(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs"))
            yield return file;

        foreach (var subDir in Directory.EnumerateDirectories(directory))
        {
            if (SkippedDirs.Contains(Path.GetFileName(subDir), StringComparer.OrdinalIgnoreCase))
                continue;

            foreach (var file in Enumerate(subDir))
                yield return file;
        }
    }
}
