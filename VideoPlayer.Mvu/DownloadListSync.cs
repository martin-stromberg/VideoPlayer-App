using System.Collections.ObjectModel;

namespace VideoPlayer.Mvu;

/// <summary>
/// Gleicht die gebundene Liste der Downloads-Seite
/// (<see cref="ObservableCollection{T}"/> von
/// <see cref="DownloadItemViewModel"/>) mit der unveränderlichen
/// Modell-Liste <c>Downloads.Items</c> ab. Bestehende Einträge werden
/// in-place aktualisiert (<see cref="DownloadItemViewModel.Update"/>) —
/// ein Fortschritts-Tick löst dadurch kein
/// <c>CollectionChanged</c>-Ereignis aus und der CollectionView wird
/// nicht zurückgesetzt. Nur Strukturänderungen (neue, entfallene oder
/// verschobene Einträge) erzeugen Add-/Remove-/Move-Ereignisse.
/// </summary>
public static class DownloadListSync
{
    /// <summary>
    /// Bringt die gebundene Liste auf den Stand der Modell-Liste.
    /// Identität eines Eintrags wie im MVU-Upsert: gleiche
    /// <c>EntryId</c> oder gleiche <c>MediaId</c>+<c>MediaType</c>.
    /// </summary>
    /// <param name="viewItems">Die gebundene Liste (ItemsSource der Seite).</param>
    /// <param name="items">Die Modell-Liste (in Manifest-Reihenfolge).</param>
    public static void Sync(
        ObservableCollection<DownloadItemViewModel> viewItems,
        IReadOnlyList<DownloadListItem> items)
    {
        ArgumentNullException.ThrowIfNull(viewItems);
        ArgumentNullException.ThrowIfNull(items);

        // 1. Entfallene Einträge entfernen, bestehende in-place
        //    aktualisieren (kein CollectionChanged bei Inhalt-Updates).
        for (var i = viewItems.Count - 1; i >= 0; i--)
        {
            var viewItem = viewItems[i];
            var updated = items.FirstOrDefault(candidate => IsSameDownload(candidate, viewItem.Item));
            if (updated is null)
            {
                viewItems.RemoveAt(i);
            }
            else
            {
                viewItem.Update(updated);
            }
        }

        // 2. Neue Einträge an ihrer Zielposition einfügen bzw.
        //    verschobene Einträge an die Zielposition bewegen.
        for (var i = 0; i < items.Count; i++)
        {
            var target = items[i];
            if (i < viewItems.Count && IsSameDownload(viewItems[i].Item, target))
            {
                continue;
            }

            var existingIndex = IndexOf(viewItems, target, i + 1);
            if (existingIndex >= 0)
            {
                viewItems.Move(existingIndex, i);
            }
            else
            {
                viewItems.Insert(i, new DownloadItemViewModel(target));
            }
        }
    }

    /// <summary>Prüft, ob zwei Einträge denselben Download bezeichnen (Regel des MVU-Upserts).</summary>
    /// <param name="a">Der eine Eintrag.</param>
    /// <param name="b">Der andere Eintrag.</param>
    /// <returns><c>true</c> bei gleicher <c>EntryId</c> oder gleicher <c>MediaId</c>+<c>MediaType</c>.</returns>
    private static bool IsSameDownload(DownloadListItem a, DownloadListItem b) =>
        a.EntryId == b.EntryId
        || (a.MediaId == b.MediaId && a.MediaType == b.MediaType);

    /// <summary>Sucht den ViewModel-Index eines Eintrags ab einer Startposition.</summary>
    /// <param name="viewItems">Die gebundene Liste.</param>
    /// <param name="target">Der gesuchte Eintrag.</param>
    /// <param name="from">Startindex der Suche.</param>
    /// <returns>Der Index oder <c>-1</c>, wenn der Eintrag nicht (mehr) vorhanden ist.</returns>
    private static int IndexOf(
        ObservableCollection<DownloadItemViewModel> viewItems,
        DownloadListItem target,
        int from)
    {
        for (var i = Math.Max(from, 0); i < viewItems.Count; i++)
        {
            if (IsSameDownload(viewItems[i].Item, target))
            {
                return i;
            }
        }

        return -1;
    }
}
