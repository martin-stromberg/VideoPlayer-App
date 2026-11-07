using System.Collections.ObjectModel;

namespace VideoPlayer.Mvu;

/// <summary>
/// Gleicht die gebundene Eintragsliste der Quellen-Übersicht
/// (<see cref="ObservableCollection{T}"/> von <see cref="SourceMediaItem"/>)
/// mit der unveränderlichen Modell-Liste <c>SourceBrowser.Items</c> ab.
/// Die gebundene Liste bleibt als ItemsSource über alle Modell-Ticks
/// bestehen; nur Strukturänderungen erzeugen
/// <c>CollectionChanged</c>-Ereignisse. Ein nachgeladener Seiten-Anhang
/// löst so ausschließlich <c>Add</c>-Ereignisse am Ende aus — die
/// CollectionView behält ihre Scrollposition, statt wie bei einem
/// ItemsSource-Referenzwechsel an den Listenanfang zu springen.
/// Identität eines Eintrags ist die <see cref="SourceMediaItem.Id"/>:
/// das gemeinsame Präfix bleibt unverändert, alles danach wird dem
/// Modellstand angepasst (Erstladen, Filter-/Suchwechsel ersetzen die
/// Liste ab der ersten abweichenden Position — gewünschter Neuaufbau).
/// </summary>
public static class SourceItemListSync
{
    /// <summary>
    /// Bringt die gebundene Liste auf den Stand der Modell-Liste.
    /// Einträge des gemeinsamen Präfixes (gleiche <see cref="SourceMediaItem.Id"/>
    /// an gleicher Position) bleiben unverändert; dahinter wird die
    /// gebundene Liste entfernt und das Modell-Ende angehängt.
    /// </summary>
    /// <param name="viewItems">Die gebundene Liste (ItemsSource der Seite).</param>
    /// <param name="items">Die Modell-Liste (kumuliert über alle geladenen Seiten).</param>
    public static void Sync(
        ObservableCollection<SourceMediaItem> viewItems,
        IReadOnlyList<SourceMediaItem> items)
    {
        ArgumentNullException.ThrowIfNull(viewItems);
        ArgumentNullException.ThrowIfNull(items);

        var prefix = 0;
        var shared = Math.Min(viewItems.Count, items.Count);
        while (prefix < shared && viewItems[prefix].Id == items[prefix].Id)
        {
            prefix++;
        }

        // Gebundenen Endabschnitt hinter dem gemeinsamen Präfix entfernen …
        for (var i = viewItems.Count - 1; i >= prefix; i--)
        {
            viewItems.RemoveAt(i);
        }

        // … und das Modell-Ende dahinter anhängen (bei reinem Anhängen
        // ausschließlich Add-Ereignisse → Scrollposition bleibt).
        for (var i = prefix; i < items.Count; i++)
        {
            viewItems.Add(items[i]);
        }
    }
}
