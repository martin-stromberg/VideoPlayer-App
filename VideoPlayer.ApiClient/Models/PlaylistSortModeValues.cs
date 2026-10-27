namespace VideoPlayer.ApiClient.Models;

/// <summary>
/// Zentrale String-Konstanten für die Sortiermodi von Playlists,
/// spiegelt den serverseitigen <c>PlaylistSortMode</c>. Dient an der
/// Client/Server-JSON-Grenze dazu, die rohen Literalwerte nicht zu
/// duplizieren. Entspricht der Referenz-Klasse
/// <c>PlaylistSortModeValues</c> des Servers.
/// </summary>
public static class PlaylistSortModeValues
{
    /// <summary>Einträge sind nach Erscheinungsdatum sortiert.</summary>
    public const string ByReleaseDate = "ByReleaseDate";

    /// <summary>Einträge sind vom Benutzer manuell sortiert.</summary>
    public const string Manual = "Manual";

    /// <summary>
    /// Liefert die deutsche Anzeigebezeichnung zum Sortiermodus
    /// („Manuell" oder „Nach Erscheinungsdatum").
    /// </summary>
    /// <param name="sortMode">Der rohe Sortiermodus-Wert, z. B. aus <see cref="Playlist.SortMode"/>.</param>
    /// <returns>Die deutsche Anzeigebezeichnung.</returns>
    public static string GetDisplayLabel(string? sortMode)
        => sortMode == Manual ? "Manuell" : "Nach Erscheinungsdatum";
}
