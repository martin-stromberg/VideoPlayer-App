using System.Collections.ObjectModel;
using System.Collections.Specialized;
using VideoPlayer.Mvu;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Granularer Aktualisierungspfad der Quellen-Eintragsliste (Issue #49,
/// Schritt 3): <see cref="SourceItemListSync"/> gleicht die gebundene
/// <see cref="ObservableCollection{T}"/> in-place mit der Modell-Liste
/// ab — eine nachgeladene Folgeseite löst nur <c>Add</c>-Ereignisse am
/// Ende aus (kein <c>Reset</c>/<c>Remove</c>), sodass die CollectionView
/// ihre Scrollposition behält; Erstladen, Genre-Wechsel und neue Suche
/// ersetzen die Liste ab der ersten abweichenden Position.
/// </summary>
public class SourceItemListSyncTests
{
    private static SourceMediaItem Item(long id) =>
        new() { Id = id, Title = $"Eintrag {id}", MediaType = "movie" };

    /// <summary>Prüft Sync: eine angehängte Folgeseite löst nur Add-Ereignisse am Ende aus.</summary>
    [Fact]
    public void Sync_AppendOnly_AddsTail_WithoutReset()
    {
        var viewItems = new ObservableCollection<SourceMediaItem>();
        var page1 = new[] { Item(1), Item(2), Item(3) };
        SourceItemListSync.Sync(viewItems, page1);
        var first = viewItems[0];
        var second = viewItems[1];
        var third = viewItems[2];

        var events = new List<NotifyCollectionChangedEventArgs>();
        viewItems.CollectionChanged += (_, e) => events.Add(e);

        // UpdateSourceItemsLoaded liefert beim Anhängen alte + neue Einträge.
        SourceItemListSync.Sync(viewItems, [.. page1, Item(4), Item(5)]);

        Assert.Equal(5, viewItems.Count);
        Assert.Same(first, viewItems[0]);
        Assert.Same(second, viewItems[1]);
        Assert.Same(third, viewItems[2]);
        Assert.Equal(4, viewItems[3].Id);
        Assert.Equal(5, viewItems[4].Id);
        // Ausschließlich Add-Ereignisse am Ende — kein Reset/Remove,
        // die Ansicht darf nicht neu aufgebaut werden (Scrollposition).
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(NotifyCollectionChangedAction.Add, e.Action));
        Assert.Equal(3, events[0].NewStartingIndex);
        Assert.Equal(4, events[1].NewStartingIndex);
    }

    /// <summary>Prüft Sync: ein abweichender erster Eintrag ersetzt die Liste (Erstladen/Filterwechsel).</summary>
    [Fact]
    public void Sync_ChangedPrefix_ReplacesFromDivergence()
    {
        var viewItems = new ObservableCollection<SourceMediaItem>();
        SourceItemListSync.Sync(viewItems, [Item(1), Item(2), Item(3)]);

        // Neue Trefferliste einer Suche — weicht ab Position 0 ab.
        SourceItemListSync.Sync(viewItems, [Item(9), Item(8)]);

        Assert.Equal(2, viewItems.Count);
        Assert.Equal(9, viewItems[0].Id);
        Assert.Equal(8, viewItems[1].Id);
    }

    /// <summary>Prüft Sync: gemeinsames Präfix bleibt stehen, nur der divergierende Rest wird ersetzt.</summary>
    [Fact]
    public void Sync_PartialPrefix_KeepsSharedHead()
    {
        var viewItems = new ObservableCollection<SourceMediaItem>();
        SourceItemListSync.Sync(viewItems, [Item(1), Item(2), Item(3)]);
        var head = viewItems[0];

        SourceItemListSync.Sync(viewItems, [Item(1), Item(7)]);

        Assert.Equal(2, viewItems.Count);
        Assert.Same(head, viewItems[0]);
        Assert.Equal(7, viewItems[1].Id);
    }

    /// <summary>Prüft Sync: eine kürzere Modell-Liste entfernt den überzähligen Endabschnitt.</summary>
    [Fact]
    public void Sync_ShorterModel_RemovesTail()
    {
        var viewItems = new ObservableCollection<SourceMediaItem>();
        SourceItemListSync.Sync(viewItems, [Item(1), Item(2), Item(3)]);
        var head = viewItems[0];

        SourceItemListSync.Sync(viewItems, [Item(1)]);

        var survivor = Assert.Single(viewItems);
        Assert.Same(head, survivor);
    }

    /// <summary>Prüft Sync: ein Modell-Tick ohne Listenänderung löst kein CollectionChanged aus.</summary>
    [Fact]
    public void Sync_Unchanged_NoNotifications()
    {
        var viewItems = new ObservableCollection<SourceMediaItem>();
        SourceItemListSync.Sync(viewItems, [Item(1), Item(2)]);
        var events = 0;
        viewItems.CollectionChanged += (_, _) => events++;

        // Neue Listeninstanz mit gleichem Inhalt (MVU-Modellablösung).
        SourceItemListSync.Sync(viewItems, [Item(1), Item(2)]);

        Assert.Equal(0, events);
    }

    /// <summary>Prüft Sync: eine leere Modell-Liste (Reset-Pfad) leert die gebundene Liste.</summary>
    [Fact]
    public void Sync_EmptyModel_Clears()
    {
        var viewItems = new ObservableCollection<SourceMediaItem>();
        SourceItemListSync.Sync(viewItems, [Item(1), Item(2)]);

        SourceItemListSync.Sync(viewItems, []);

        Assert.Empty(viewItems);
    }
}
