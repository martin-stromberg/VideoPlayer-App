namespace VideoPlayer.Maui.Tests.Stubs;

/// <summary>
/// Findet das Repository-Wurzelverzeichnis (Verzeichnis, das
/// `VideoPlayer.App.sln` enthält) ausgehend vom Test-Ausführungsverzeichnis.
/// </summary>
internal static class RepositoryRoot
{
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VideoPlayer.App.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
