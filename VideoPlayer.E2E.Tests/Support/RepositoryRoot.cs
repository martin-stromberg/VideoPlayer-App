namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// Findet das Repository-Wurzelverzeichnis (Verzeichnis, das
/// `VideoPlayer.App.sln` enthält) ausgehend vom Test-Ausführungsverzeichnis.
/// </summary>
public static class RepositoryRoot
{
    /// <summary>Liefert den absoluten Pfad der Repository-Wurzel.</summary>
    /// <returns>Das Wurzelverzeichnis.</returns>
    /// <exception cref="DirectoryNotFoundException">Die Wurzel konnte nicht ermittelt werden.</exception>
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
