using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoPlayer.ApiClient.Models;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>
/// Gecachter Eintrag einer Playlist (Offline-Playlist-Kontext des
/// Auto-Download-Vorlaufs und der Bereinigungs-Kandidatenprüfung).
/// </summary>
public sealed record CachedPlaylistEntry
{
    /// <summary>Id des Playlist-Eintrags.</summary>
    public required long EntryId { get; init; }

    /// <summary>Medientyp des referenzierten Mediums (<c>movie</c>, <c>tvshowepisode</c>, …).</summary>
    public required string MediaType { get; init; }

    /// <summary>Id des referenzierten Mediums.</summary>
    public required long MediaId { get; init; }

    /// <summary>Sortierposition des Eintrags in der Playlist; <c>null</c> = nicht zugewiesen.</summary>
    public long? SortOrder { get; init; }

    /// <summary>Zugriffsrecht auf das Medium (Filter des Auto-Download-Vorlaufs).</summary>
    public bool IsAccessible { get; init; }
}

/// <summary>
/// Gecachte Playlist-Struktur: die geordneten Einträge einer Playlist,
/// geschrieben beim Auto-Download-Vorlauf. Grundlage der offline-fähigen
/// Playlist-Nachfolger-Prüfung des Bereinigungslaufs.
/// </summary>
public sealed record CachedPlaylistEntries
{
    /// <summary>Id der Playlist.</summary>
    public required long PlaylistId { get; init; }

    /// <summary>Einträge der Playlist in Reihenfolge ihrer Sortierposition.</summary>
    public IReadOnlyList<CachedPlaylistEntry> Entries { get; init; } = [];
}

/// <summary>
/// Lese-/Schreibzugang der gecachten Playlist-Struktur in
/// <c>cached_lists</c> (Schlüssel <c>playlist:{id}</c>) — Muster wie
/// <see cref="ShowDetailsCache"/>. Die Ablage ist ein nicht
/// backup-pflichtiger Cache; Lesefehler liefern <c>null</c> statt eine
/// Ausnahme.
/// </summary>
public static class PlaylistEntriesCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Cache-Schlüssel einer Playlist.</summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <returns>Der <c>cached_lists</c>-Schlüssel im Format <c>playlist:{id}</c>.</returns>
    public static string KeyFor(long playlistId) => $"playlist:{playlistId}";

    /// <summary>
    /// Schreibt die geladenen Playlist-Einträge in ihrer serverseitigen
    /// Reihenfolge in den Cache. Schreibfehler werden protokolliert und
    /// übergangen — der Cache ist eine Komfortfunktion, kein
    /// Pflichtbestand.
    /// </summary>
    /// <param name="cache">Die Cache-Ablage.</param>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="entries">Die geladenen Playlist-Einträge (Serverantworten).</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll (Fehlerpfad).</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Schreiben.</returns>
    public static async Task WriteAsync(
        CacheDatabase cache,
        long playlistId,
        IReadOnlyList<PlaylistEntry> entries,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(entries);
        if (playlistId <= 0)
        {
            return;
        }

        var cached = new CachedPlaylistEntries
        {
            PlaylistId = playlistId,
            Entries = entries
                .Select(e => new CachedPlaylistEntry
                {
                    EntryId = e.Id,
                    MediaType = e.MediaType,
                    MediaId = e.MediaId,
                    SortOrder = e.SortOrder,
                    IsAccessible = e.IsAccessible
                })
                .ToList()
        };

        try
        {
            var row = new CachedListEntry
            {
                Key = KeyFor(playlistId),
                Json = JsonSerializer.Serialize(cached),
                UpdatedAtUtc = DateTime.UtcNow
            };
            await cache.Connection.InsertOrReplaceAsync(row);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Schreiben des Playlist-Caches '{CacheKey}' fehlgeschlagen.", KeyFor(playlistId));
        }
    }

    /// <summary>
    /// Liest die gecachte Playlist-Struktur. Fehlende oder beschädigte
    /// Einträge liefern <c>null</c>.
    /// </summary>
    /// <param name="cache">Die Cache-Ablage.</param>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll (Fehlerpfad).</param>
    /// <returns>Die gecachte Playlist-Struktur; <c>null</c> bei Fehlen oder Lesefehler.</returns>
    public static async Task<CachedPlaylistEntries?> ReadAsync(
        CacheDatabase cache,
        long playlistId,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        // Den Schlüssel vorab berechnen: sqlite-net kann den Methodenaufruf
        // nicht in SQL übersetzen („no such function: keyfor").
        var key = KeyFor(playlistId);
        try
        {
            var row = await cache.Connection.Table<CachedListEntry>()
                .Where(e => e.Key == key)
                .FirstOrDefaultAsync();
            if (row?.Json is not { Length: > 0 } json)
            {
                return null;
            }

            var entry = JsonSerializer.Deserialize<CachedPlaylistEntries>(json, JsonOptions);
            return entry?.PlaylistId > 0 ? entry : null;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Lesen des Playlist-Caches '{CacheKey}' fehlgeschlagen.", KeyFor(playlistId));
            return null;
        }
    }
}
