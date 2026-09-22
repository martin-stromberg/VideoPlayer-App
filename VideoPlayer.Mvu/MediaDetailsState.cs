namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand der Medien-Detailseite: angefragtes Medium
/// (<see cref="MediaType"/>/<see cref="MediaId"/>), Lade-/Fehlerzustand,
/// geladenes Anzeigemodell sowie der Zustand einer laufenden
/// Favoriten-Umschaltung.
/// </summary>
public sealed record MediaDetailsState
{
    /// <summary>Ausgangszustand (kein Medium angefragt).</summary>
    /// <value>Der leere Ausgangszustand der Detailseite.</value>
    public static MediaDetailsState Empty { get; } = new();

    /// <summary>Angefragter Medientyp (normalisiert, CLR-Konvention).</summary>
    public string? MediaType { get; init; }

    /// <summary>Angefragte Medien-Id.</summary>
    public long MediaId { get; init; }

    /// <summary>Die Detaildaten werden gerade geladen.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Mindestens eine Anfrage wurde gestellt.</summary>
    public bool Attempted { get; init; }

    /// <summary>Fehlertext des letzten Ladeversuchs; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Eine Favoriten-Umschaltung läuft gerade (Stern deaktiviert).</summary>
    public bool IsFavoriteUpdating { get; init; }

    /// <summary>Fehlertext der letzten Favoriten-Umschaltung; <c>null</c> = kein Fehler.</summary>
    public string? FavoriteError { get; init; }

    /// <summary>
    /// Hinweistext der letzten Download-Anforderung dieses Mediums
    /// (eingeplant/bereits vorhanden/Fehler); <c>null</c> = kein Hinweis.
    /// </summary>
    public string? DownloadNotice { get; init; }

    /// <summary><c>true</c>, wenn <see cref="DownloadNotice"/> einen Fehler/eine Ablehnung beschreibt.</summary>
    public bool DownloadNoticeIsError { get; init; }

    /// <summary>Geladenes Anzeigemodell; <c>null</c> während des Ladens oder bei Fehler.</summary>
    public MediaDetailsView? Details { get; init; }

    /// <summary>
    /// Bilddaten des Kopfbereichs (flüchtig, kein Dateicache);
    /// <c>null</c> = noch nicht geladen/fehlgeschlagen — der Kopfbereich
    /// zeigt dann nur Gradienten und Texte.
    /// </summary>
    public byte[]? HeaderImageBytes { get; init; }

    /// <summary>Das Kopfbereichs-Bild wird gerade geladen.</summary>
    public bool IsHeaderImageLoading { get; init; }

    /// <summary>Ein Kopfbereichs-Bild liegt vor.</summary>
    public bool HasHeaderImage => HeaderImageBytes is { Length: > 0 };

    /// <summary>Detaildaten liegen vor.</summary>
    public bool HasDetails => Details is not null;

    /// <summary>Der letzte Ladeversuch ist fehlgeschlagen.</summary>
    public bool HasError => Error is not null;
}
