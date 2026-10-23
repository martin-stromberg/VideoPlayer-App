namespace VideoPlayer.Mvu;

/// <summary>
/// Zentrale Zuordnung zwischen Menüseiten und Shell-Routen. Die Route-Namen
/// sind zugleich die `Route`-Werte in `AppShell.xaml` (per Contract-Test
/// abgesichert).
/// </summary>
public static class AppRoutes
{
    private static readonly IReadOnlyDictionary<AppPage, string> Routes =
        new Dictionary<AppPage, string>
        {
            [AppPage.Home] = "home",
            [AppPage.Favorites] = "favorites",
            [AppPage.Playlists] = "playlists",
            [AppPage.Downloads] = "downloads",
            [AppPage.Profile] = "profile",
            [AppPage.Settings] = "settings"
        };

    /// <summary>
    /// Registrierte Shell-Routen ohne Menüeintrag. Sie werden auf den
    /// Navigationsstack geschoben und erscheinen im Shell-Ort hinter dem
    /// Ort des Menüeintrags (z. B. <c>//settings/serversetup</c>).
    /// </summary>
    private static readonly IReadOnlyDictionary<AppPage, string> PushedRoutes =
        new Dictionary<AppPage, string>
        {
            [AppPage.ServerSetup] = "serversetup",
            [AppPage.Login] = "login"
        };

    /// <summary>Alle Menüseiten in Reihenfolge des Hauptmenüs.</summary>
    public static IReadOnlyList<AppPage> MenuPages { get; } =
    [
        AppPage.Home,
        AppPage.Favorites,
        AppPage.Playlists,
        AppPage.Downloads,
        AppPage.Profile,
        AppPage.Settings
    ];

    /// <summary>Liefert die Shell-Route einer Menüseite.</summary>
    /// <param name="page">Die Menüseite.</param>
    /// <returns>Der Routenname der Seite.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Für die Seite existiert keine Menü-Route.</exception>
    public static string ToRoute(AppPage page)
    {
        if (Routes.TryGetValue(page, out var route))
        {
            return route;
        }

        throw new ArgumentOutOfRangeException(nameof(page), page, "Für diese Seite existiert keine Menü-Route.");
    }

    /// <summary>
    /// Ermittelt zu einem Shell-Navigationsziel (z. B. <c>//favorites/FavoritesPage</c>
    /// oder <c>//settings/serversetup</c>) die angezeigte Seite.
    /// </summary>
    /// <param name="location">Das Shell-Navigationsziel (Pfad mit optionalen Query-Parametern).</param>
    /// <param name="page">Die ermittelte Seite; bei <c>false</c> der Standardwert.</param>
    /// <returns><c>true</c>, wenn ein Pfadsegment einer bekannten Route entspricht.</returns>
    /// <remarks>
    /// Geschobene Routen hängen am Ort des darunterliegenden Menüeintrags —
    /// bei mehreren Treffern bestimmt das tiefste Segment die angezeigte
    /// Seite (<c>//settings/serversetup</c> → <see cref="AppPage.ServerSetup"/>).
    /// </remarks>
    public static bool TryGetPage(string? location, out AppPage page)
    {
        page = default;
        if (string.IsNullOrWhiteSpace(location))
        {
            return false;
        }

        var found = false;
        var path = location.Split('?')[0];
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryResolveSegment(segment, Routes, out var resolved)
                || TryResolveSegment(segment, PushedRoutes, out resolved))
            {
                page = resolved;
                found = true;
            }
        }

        return found;
    }

    /// <summary>Ordnet ein Pfadsegment einer Route aus der angegebenen Tabelle zu.</summary>
    /// <param name="segment">Das Pfadsegment.</param>
    /// <param name="routes">Die Route-Tabelle (Seite → Routenname).</param>
    /// <param name="page">Die ermittelte Seite.</param>
    /// <returns><c>true</c>, wenn das Segment einer Route entspricht.</returns>
    private static bool TryResolveSegment(
        string segment,
        IReadOnlyDictionary<AppPage, string> routes,
        out AppPage page)
    {
        foreach (var pair in routes)
        {
            if (string.Equals(segment, pair.Value, StringComparison.OrdinalIgnoreCase))
            {
                page = pair.Key;
                return true;
            }
        }

        page = default;
        return false;
    }
}
