using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.Mvu;

/// <summary>
/// Anzeigefertiger Eintrag der Playlists-Übersicht (Menüseite
/// „Playlists") — eine eigene oder eine öffentliche Playlist des
/// Servers. <see cref="CoverPictureId"/> steuert, ob die Karte das
/// Cover (<c>api/playlists/{id}/cover</c>) oder den Titel-Fallback
/// zeigt.
/// </summary>
public sealed record PlaylistListItem
{
    /// <summary>Id der Playlist auf dem Server (Navigationsziel der Detailseite).</summary>
    public required long PlaylistId { get; init; }

    /// <summary>Name der Playlist.</summary>
    public required string Name { get; init; }

    /// <summary>Beschreibung der Playlist; <c>null</c> = keine vorhanden.</summary>
    public string? Description { get; init; }

    /// <summary>Sortiermodus der Playlist (<see cref="PlaylistSortModeValues"/>).</summary>
    public string SortMode { get; init; } = PlaylistSortModeValues.ByReleaseDate;

    /// <summary>
    /// Id des Cover-Bilds; <c>null</c> = kein Cover — die Karte zeigt
    /// den Namen als Fallback.
    /// </summary>
    public long? CoverPictureId { get; init; }

    /// <summary>Kommaseparierte Anzeige-Genres der Playlist; leer = keine.</summary>
    public string GenreText { get; init; } = string.Empty;

    /// <summary>Die Playlist stammt aus der Liste der öffentlichen Playlists.</summary>
    public bool IsPublic { get; init; }

    /// <summary>Der angemeldete Benutzer ist Besitzer der Playlist.</summary>
    public bool IsOwner { get; init; }

    /// <summary>Ein Cover kann geladen werden.</summary>
    public bool HasCover => CoverPictureId is > 0;

    /// <summary>Playlist-Id für den Cover-Abruf; <c>null</c>, wenn kein Cover existiert (verhindert 404-Abrufe).</summary>
    public long? CoverPlaylistId => HasCover ? PlaylistId : null;

    /// <summary>Deutsche Anzeigebezeichnung des Sortiermodus.</summary>
    public string SortModeText =>
        PlaylistSortModeValues.GetDisplayLabel(SortMode);

    /// <summary>Anzeige-Genres liegen vor.</summary>
    public bool HasGenreText => GenreText.Length > 0;
}

/// <summary>
/// Flache Listenzeile der Playlists-Übersicht: entweder eine
/// Sektionsüberschrift (<see cref="IsHeader"/> = <see langword="true"/>)
/// oder eine Playlist-Karte (<see cref="Playlist"/> belegt). Eine
/// einzige scrollbare Liste deckt beide Sektionen ab — zwei getrennte
/// Listen scrollten nur je in ihrem Teilbereich.
/// </summary>
public sealed record PlaylistOverviewItem
{
    /// <summary>Die Zeile ist eine Sektionsüberschrift (kein Eintrag).</summary>
    public bool IsHeader { get; init; }

    /// <summary>Die Zeile ist eine Playlist-Karte (kein Header).</summary>
    public bool IsEntry => !IsHeader;

    /// <summary>Anzeigetext der Zeile (Sektionstitel bzw. Playlist-Name — auch Anzeigetext der Listen-Diagnose).</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Die Playlist der Zeile; <c>null</c> bei Sektionsüberschriften.</summary>
    public PlaylistListItem? Playlist { get; init; }

    /// <summary>Erzeugt eine Sektionsüberschrift.</summary>
    /// <param name="title">Der Sektionstitel.</param>
    /// <returns>Die Header-Zeile.</returns>
    public static PlaylistOverviewItem Header(string title) => new() { IsHeader = true, Title = title };

    /// <summary>Erzeugt eine Playlist-Zeile.</summary>
    /// <param name="playlist">Die Playlist der Zeile.</param>
    /// <returns>Die Eintragszeile.</returns>
    public static PlaylistOverviewItem Entry(PlaylistListItem playlist) =>
        new() { Title = playlist.Name, Playlist = playlist };
}

/// <summary>
/// Zustand der Playlists-Übersichtsseite (Menüseite „Playlists"): die
/// flache Zeilenliste aus eigenen und öffentlichen Playlists samt
/// Lade-/Fehlerzustand. Playlists sind ein reines Online-Feature —
/// ohne Serververbindung zeigt die Seite eine verständliche
/// Fehleranzeige (<see cref="Error"/>), keinen Cache-Stand.
/// </summary>
public sealed record PlaylistsPageState
{
    /// <summary>Ausgangszustand (nichts geladen, kein Fehler).</summary>
    /// <value>Der leere Ausgangszustand der Übersichtsseite.</value>
    public static PlaylistsPageState Initial { get; } = new();

    /// <summary>Sektionstitel der eigenen Playlists.</summary>
    public const string OwnSectionTitle = "Meine Playlists";

    /// <summary>Sektionstitel der öffentlichen Playlists.</summary>
    public const string PublicSectionTitle = "Öffentliche Playlists";

    /// <summary>Flache Zeilenliste (Sektionsüberschriften + Playlist-Karten).</summary>
    public IReadOnlyList<PlaylistOverviewItem> Items { get; init; } = [];

    /// <summary>Die Playlists werden gerade (neu) geladen.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Mindestens ein Ladeversuch wurde abgeschlossen.</summary>
    public bool Attempted { get; init; }

    /// <summary>Fehlertext des letzten Ladeversuchs; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Die Liste enthält mindestens eine Playlist (Header zählen nicht).</summary>
    public bool HasItems =>
        Items.Any(i => !i.IsHeader);

    /// <summary>Der letzte Ladeversuch ist fehlgeschlagen.</summary>
    public bool HasError => Error is not null;

    /// <summary>Erfolgreich geladen, aber weder eigene noch öffentliche Playlists vorhanden.</summary>
    public bool ShowEmpty => Attempted && !IsLoading && !HasError && !HasItems;

    /// <summary>
    /// Baut die flache Zeilenliste aus beiden Sektionen: jede nicht
    /// leere Sektion bekommt ihre Überschrift vorangestellt.
    /// </summary>
    /// <param name="own">Die eigenen Playlists.</param>
    /// <param name="publicPlaylists">Die öffentlichen Playlists.</param>
    /// <returns>Die flache Zeilenliste.</returns>
    public static IReadOnlyList<PlaylistOverviewItem> BuildItems(
        IReadOnlyList<PlaylistListItem> own,
        IReadOnlyList<PlaylistListItem> publicPlaylists)
    {
        var items = new List<PlaylistOverviewItem>(own.Count + publicPlaylists.Count + 2);
        if (own.Count > 0)
        {
            items.Add(PlaylistOverviewItem.Header(OwnSectionTitle));
            items.AddRange(own.Select(PlaylistOverviewItem.Entry));
        }

        if (publicPlaylists.Count > 0)
        {
            items.Add(PlaylistOverviewItem.Header(PublicSectionTitle));
            items.AddRange(publicPlaylists.Select(PlaylistOverviewItem.Entry));
        }

        return items;
    }
}
