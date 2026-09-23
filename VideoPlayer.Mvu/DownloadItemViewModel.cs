using System.ComponentModel;

namespace VideoPlayer.Mvu;

/// <summary>
/// Gebundener Eintrag der Downloads-Liste am Seitenrand: kapselt das
/// unveränderliche <see cref="DownloadListItem"/> aus dem MVU-Modell und
/// meldet Änderungen über <see cref="INotifyPropertyChanged"/>. Dadurch
/// kann ein Fortschritts-Tick (<c>DownloadItemChangedMessage</c>) den
/// betroffenen Listeneintrag in-place aktualisieren, ohne dass die
/// gesamte Liste neu aufgebaut wird.
/// </summary>
public sealed class DownloadItemViewModel : INotifyPropertyChanged
{
    private DownloadListItem item;

    /// <summary>Erstellt ein Eintrags-ViewModel zum angegebenen Download.</summary>
    /// <param name="item">Der anzuzeigende Download-Eintrag.</param>
    public DownloadItemViewModel(DownloadListItem item)
    {
        this.item = item ?? throw new ArgumentNullException(nameof(item));
    }

    /// <summary>Benachrichtigt Bindings über geänderte Anzeige-Properties.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Der aktuell gekapselte (unveränderliche) Download-Eintrag.</summary>
    /// <value>Der zuletzt per <see cref="Update"/> übernommene Eintrag.</value>
    public DownloadListItem Item => item;

    /// <summary>Anzeigetitel des Downloads.</summary>
    public string Title => item.Title;

    /// <summary>Übersetzte Medientyp-Bezeichnung (Anzeige neben dem Zustand).</summary>
    public string MediaTypeText => item.MediaTypeText;

    /// <summary>Übersetzter Zustandstext (Wartend, Läuft, Fertig, Fehler, Abgebrochen).</summary>
    public string StateText => item.StateText;

    /// <summary>Fortschritt im Bereich 0–1 (bei unbekannter Gesamtgröße 0).</summary>
    public double Progress => item.Progress;

    /// <summary>Prozentangabe des Fortschritts (z. B. „42 %").</summary>
    public string ProgressPercentText => item.ProgressPercentText;

    /// <summary>Der Fortschrittsbalken wird angezeigt (nur laufende Downloads).</summary>
    public bool ShowProgress => item.ShowProgress;

    /// <summary>Ein Fehlerhinweis wird angezeigt.</summary>
    public bool HasError => item.HasError;

    /// <summary>Dem Anwender anzeigbarer Fehlertext; <c>null</c>, wenn kein Fehler vorliegt.</summary>
    public string? ErrorText => item.ErrorText;

    /// <summary>Der Abbrechen-Schalter des Eintrags ist wirksam (nur aktive Downloads).</summary>
    public bool CanCancel => item.CanCancel;

    /// <summary>Der Wiederholen-Schalter des Eintrags ist wirksam (fehlgeschlagen oder abgebrochen).</summary>
    public bool CanRetry => item.CanRetry;

    /// <summary>Der Löschen-Schalter des Eintrags ist wirksam (immer).</summary>
    public bool CanDelete => item.CanDelete;

    /// <summary>
    /// Übernimmt einen geänderten Eintrag desselben Downloads (Identität
    /// über <c>EntryId</c> bzw. <c>MediaId</c>+<c>MediaType</c>). Bei
    /// Record-Gleichheit passiert nichts — ein Fortschritts-Tick ohne
    /// anzeigerelevante Änderung erzeugt keine Benachrichtigung.
    /// </summary>
    /// <param name="updated">Der geänderte Eintrag aus dem Modell.</param>
    /// <returns><c>true</c>, wenn der Eintrag übernommen und Bindings benachrichtigt wurden.</returns>
    public bool Update(DownloadListItem updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        if (updated.Equals(item))
        {
            return false;
        }

        item = updated;

        // Record-Gleichheit sagt nur „etwas hat sich geändert" — die
        // gebundenen Anzeigewerte werden pauschal gemeldet; Bindings mit
        // unverändertem Wert schreiben denselben Wert erneut (no-op).
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Item)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MediaTypeText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StateText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressPercentText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowProgress)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanCancel)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRetry)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDelete)));
        return true;
    }
}
