namespace VideoPlayer.Mvu;

/// <summary>
/// Zustand der Downloads-Seite (Menüseite „Downloads"): die Einträge des
/// Download-Manifests samt Lade-/Fehlerzustand und der Belegung des
/// konfigurierten Speicherplatzkontingents. Die Seite arbeitet ohne
/// Serververbindung — die Daten liegen lokal vor.
/// </summary>
public sealed record DownloadsPageState
{
    /// <summary>Ausgangszustand (nichts geladen, kein Fehler).</summary>
    /// <value>Der leere Ausgangszustand der Downloads-Seite.</value>
    public static DownloadsPageState Initial { get; } = new();

    /// <summary>Die Download-Liste wird gerade (neu) geladen.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Mindestens ein Ladeversuch wurde abgeschlossen.</summary>
    public bool Attempted { get; init; }

    /// <summary>Fehlertext des letzten Ladelaufs; <c>null</c> = kein Fehler.</summary>
    public string? Error { get; init; }

    /// <summary>Download-Einträge (Stand des Download-Manifests, in Auftragsreihenfolge).</summary>
    public IReadOnlyList<DownloadListItem> Items { get; init; } = [];

    /// <summary>Durch Download-Dateien belegter Speicher in Bytes.</summary>
    public long UsedBytes { get; init; }

    /// <summary>Konfiguriertes Speicherplatzkontingent in Gigabyte.</summary>
    public int QuotaGb { get; init; } = Storage.AppSettings.DefaultDownloadQuotaGb;

    /// <summary>Bytes erkannter Restdaten ohne Manifest-Eintrag (Abgleich beim Laden).</summary>
    public long OrphanedBytes { get; init; }

    /// <summary>Die Liste enthält Einträge.</summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>Der letzte Ladelauf ist fehlgeschlagen.</summary>
    public bool HasError => Error is not null;

    /// <summary>Erfolgreich geladen, aber keine Downloads vorhanden.</summary>
    public bool ShowEmpty => Attempted && !IsLoading && !HasError && !HasItems;

    /// <summary>Anzeigezeile der Kontingent-Belegung (z. B. „1,2 GB von 5 GB belegt").</summary>
    public string QuotaText => $"{UsedBytes / 1_073_741_824d:0.0} GB von {QuotaGb} GB belegt";

    /// <summary>Hinweiszeile zu erkannten Restdaten ohne Manifest-Eintrag; <c>null</c>, wenn keine vorliegen.</summary>
    public string? OrphanedText =>
        OrphanedBytes > 0
            ? $"davon {OrphanedBytes / 1_073_741_824d:0.0} GB nicht zugeordnet"
            : null;
}
