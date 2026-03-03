using System.Text.RegularExpressions;

namespace ScatGirl.Core;

static class FileScanner
{
    static readonly HashSet<string> SkippedDirs =
        new(["bin", "obj", ".vs", ".git", ".github", "node_modules", ".nuke"], StringComparer.OrdinalIgnoreCase);

    internal static IEnumerable<string> GetCSharpFiles(string rootPath, string? globFilter = null)
    {
        if (!Directory.Exists(rootPath))
            return [];

        if (globFilter is null)
            return Enumerate(rootPath);

        var absRoot = Path.GetFullPath(rootPath);
        var regex   = GlobToRegex(globFilter);

        return Enumerate(rootPath)
            .Where(f =>
            {
                var relPath = Path.GetRelativePath(absRoot, f).Replace('\\', '/');
                return regex.IsMatch(relPath);
            });
    }

    static IEnumerable<string> Enumerate(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs"))
            yield return file;

        foreach (var subDir in Directory.EnumerateDirectories(directory))
        {
            if (SkippedDirs.Contains(Path.GetFileName(subDir)))
                continue;

            foreach (var file in Enumerate(subDir))
                yield return file;
        }
    }

    static Regex GlobToRegex(string glob)
    {
        var escGlob = Regex.Escape(glob.Replace('\\', '/'))
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", ".");
        // If our pattern matches against .*/, we have to make it optional as
        // paths may or may not start with ./
        if (escGlob.StartsWith(".*/"))
            escGlob = "(?:.*/)?" + escGlob[3..];
        var pattern = "^" + escGlob + "$";
        return new(pattern, RegexOptions.IgnoreCase);
    }
}
