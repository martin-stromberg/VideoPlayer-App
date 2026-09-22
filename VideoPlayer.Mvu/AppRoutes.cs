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
            [AppPage.Sources] = "sources",
            [AppPage.Favorites] = "favorites",
            [AppPage.Downloads] = "downloads",
            [AppPage.Profile] = "profile",
            [AppPage.Settings] = "settings"
        };

    /// <summary>Alle Menüseiten in Reihenfolge des Hauptmenüs.</summary>
    public static IReadOnlyList<AppPage> MenuPages { get; } =
    [
        AppPage.Home,
        AppPage.Sources,
        AppPage.Favorites,
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
    /// Ermittelt zu einem Shell-Navigationsziel (z. B. <c>//sources/SourcesPage</c>)
    /// die zugehörige Menüseite.
    /// </summary>
    /// <param name="location">Das Shell-Navigationsziel (Pfad mit optionalen Query-Parametern).</param>
    /// <param name="page">Die ermittelte Menüseite; bei <c>false</c> der Standardwert.</param>
    /// <returns><c>true</c>, wenn ein Pfadsegment einer Menü-Route entspricht.</returns>
    public static bool TryGetPage(string? location, out AppPage page)
    {
        page = default;
        if (string.IsNullOrWhiteSpace(location))
        {
            return false;
        }

        var path = location.Split('?')[0];
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var pair in Routes)
            {
                if (string.Equals(segment, pair.Value, StringComparison.OrdinalIgnoreCase))
                {
                    page = pair.Key;
                    return true;
                }
            }
        }

        return false;
    }
}
