using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand des Playlist-Formulars (Anlage auf der Übersichtsseite,
/// Bearbeitung auf der Detailseite): das geteilte Overlay-Panel
/// befüllt die Felder über Messages; <see cref="EditingPlaylistId"/>
/// unterscheidet Anlage (<c>null</c>) und Bearbeitung.
/// </summary>
public sealed record PlaylistFormState
{
    /// <summary>Ausgangszustand (Formular geschlossen).</summary>
    /// <value>Der geschlossene Ausgangszustand.</value>
    public static PlaylistFormState Closed { get; } = new();

    /// <summary>Das Formular ist geöffnet.</summary>
    public bool IsOpen { get; init; }

    /// <summary>Id der bearbeiteten Playlist; <c>null</c> = Neuanlage.</summary>
    public long? EditingPlaylistId { get; init; }

    /// <summary>Aktueller Inhalt des Namensfelds.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Aktueller Inhalt des Beschreibungsfelds.</summary>
    public string? Description { get; init; }

    /// <summary>Gewählter Sortiermodus (nur bei Neuanlage wählbar).</summary>
    public string SortMode { get; init; } = PlaylistSortModeValues.Manual;

    /// <summary>Ein Speichervorgang läuft (Felder/Schalter gesperrt).</summary>
    public bool IsSaving { get; init; }

    /// <summary>Inline-Fehlertext des Formulars; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Bearbeitungsmodus (bestehende Playlist).</summary>
    public bool IsEditing => EditingPlaylistId is > 0;
}

/// <summary>
/// Auswählbare Genre-Option des Genre-Editors (Klartext-Name,
/// niemals eine eingegebene Kennung).
/// </summary>
public sealed record PlaylistGenreOptionItem
{
    /// <summary>Id des Genres.</summary>
    public required long Id { get; init; }

    /// <summary>Anzeigename des Genres.</summary>
    public required string Name { get; init; }

    /// <summary>Das Genre ist gewählt.</summary>
    public bool IsSelected { get; init; }

    /// <summary>Eindeutige AutomationId des Auswahlschalters.</summary>
    public string OptionAutomationId => $"GenreOption_{Id}";
}

/// <summary>
/// Zustand des Genre-Editors einer Playlist: lädt die
/// Genre-Optionen des Servers (<c>api/items/genres</c>) mit der
/// Vorauswahl der aktuellen Playlist-Genres und speichert die
/// Auswahl über <c>PUT api/playlists/{id}/genres</c> bzw. setzt sie
/// über <c>POST api/playlists/{id}/genres/reset</c> zurück.
/// </summary>
public sealed record PlaylistGenreEditorState
{
    /// <summary>Ausgangszustand (Editor geschlossen).</summary>
    /// <value>Der geschlossene Ausgangszustand.</value>
    public static PlaylistGenreEditorState Closed { get; } = new();

    /// <summary>Der Editor ist geöffnet.</summary>
    public bool IsOpen { get; init; }

    /// <summary>Id der Playlist, deren Genres bearbeitet werden.</summary>
    public long PlaylistId { get; init; }

    /// <summary>Genre-Optionen mit Auswahlzustand.</summary>
    public IReadOnlyList<PlaylistGenreOptionItem> Options { get; init; } = [];

    /// <summary>Die Optionen werden gerade geladen.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Ein Speicher-/Reset-Vorgang läuft.</summary>
    public bool IsSaving { get; init; }

    /// <summary>Inline-Fehlertext des Editors; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Ids der aktuell gewählten Genres.</summary>
    public IReadOnlyList<long> SelectedIds
    {
        get
        {
            List<long> ids = [];
            foreach (var option in Options)
            {
                if (option.IsSelected)
                {
                    ids.Add(option.Id);
                }
            }

            return ids;
        }
    }
}

/// <summary>
/// Ergebnis eines Verwaltungsaufrufs, der serverseitig einen
/// Bestätigungskonflikt (HTTP 409) melden kann: bei
/// <see cref="RequiresConfirmation"/> liegt kein Fehler vor — die
/// Anfrage wurde nur zurückgestellt, bis der Anwender die
/// Bestätigung erteilt hat.
/// </summary>
/// <param name="Succeeded"><c>true</c> = der Aufruf wurde ausgeführt.</param>
/// <param name="RequiresConfirmation"><c>true</c> = der Server verlangt eine Bestätigung (409).</param>
/// <returns>Datensatz des Ergebnisses.</returns>
public sealed record PlaylistMutationOutcome(bool Succeeded, bool RequiresConfirmation);

/// <summary>
/// Ergebnis des Sortiermodus-Wechsels: bei Erfolg die aktualisierte
/// Playlist, bei <paramref name="RequiresConfirmation"/> der
/// serverseitige Verlust-Konflikt (HTTP 409) — die Anfrage kann mit
/// Bestätigungsflagge erneut werden.
/// </summary>
/// <param name="Playlist">Die aktualisierte Playlist; <c>null</c> bei Konflikt.</param>
/// <param name="RequiresConfirmation"><c>true</c> = der Server verlangt die Verlust-Bestätigung.</param>
/// <returns>Datensatz des Wechselergebnisses.</returns>
public sealed record PlaylistSortModeChangeResult(
    PlaylistListItem? Playlist,
    bool RequiresConfirmation);
