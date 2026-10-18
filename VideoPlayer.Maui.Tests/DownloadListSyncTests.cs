using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Granularer Aktualisierungspfad der Downloads-Liste (Schritt 2):
/// <see cref="DownloadListSync"/> gleicht die gebundene
/// <see cref="ObservableCollection{T}"/> in-place mit der Modell-Liste
/// ab — Fortschritts-Ticks lösen nur <c>PropertyChanged</c> am
/// betroffenen <see cref="DownloadItemViewModel"/> aus, kein
/// <c>CollectionChanged</c> und damit kein Neuaufbau der Liste
/// (Flackern).
/// </summary>
public class DownloadListSyncTests
{
    private static DownloadListItem Item(
        long mediaId = 42,
        string mediaType = "movie",
        DownloadState state = DownloadState.Downloading,
        int entryId = 0) => new()
        {
            EntryId = entryId > 0 ? entryId : (int)mediaId,
            MediaType = mediaType,
            MediaId = mediaId,
            Title = $"Titel {mediaId}",
            State = state
        };

    /// <summary>Prüft Sync: neue Einträge werden als ViewModels angelegt.</summary>
    [Fact]
    public void Sync_NewItems_InsertsViewModels()
    {
        var viewItems = new ObservableCollection<DownloadItemViewModel>();

        DownloadListSync.Sync(viewItems, [Item(1), Item(2)]);

        Assert.Equal(2, viewItems.Count);
        Assert.Equal(1, viewItems[0].Item.MediaId);
        Assert.Equal(2, viewItems[1].Item.MediaId);
    }

    /// <summary>Prüft Sync: ein Fortschritts-Tick aktualisiert nur den Eintrag — kein CollectionChanged.</summary>
    [Fact]
    public void Sync_ProgressTick_UpdatesInPlace_WithoutCollectionChanged()
    {
        var viewItems = new ObservableCollection<DownloadItemViewModel>();
        DownloadListSync.Sync(viewItems, [Item(1), Item(42), Item(3)]);
        var viewItem = viewItems[1];

        var collectionEvents = new List<NotifyCollectionChangedEventArgs>();
        viewItems.CollectionChanged += (_, e) => collectionEvents.Add(e);
        var propertyChanges = new List<string?>();
        viewItem.PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);

        var updated = Item(42) with { DownloadedBytes = 50, TotalBytes = 100 };
        DownloadListSync.Sync(viewItems, [Item(1), updated, Item(3)]);

        Assert.Empty(collectionEvents);
        Assert.Same(viewItem, viewItems[1]);
        Assert.Same(updated, viewItem.Item);
        Assert.Equal(0.5, viewItem.Progress, 3);
        Assert.Contains(nameof(DownloadItemViewModel.Progress), propertyChanges);
        Assert.Contains(nameof(DownloadItemViewModel.ProgressPercentText), propertyChanges);
    }

    /// <summary>Prüft Sync: unveränderte Einträge erzeugen keinerlei Benachrichtigungen.</summary>
    [Fact]
    public void Sync_UnchangedItems_NoNotifications()
    {
        var viewItems = new ObservableCollection<DownloadItemViewModel>();
        DownloadListSync.Sync(viewItems, [Item(42)]);
        var viewItem = viewItems[0];

        var collectionEvents = 0;
        viewItems.CollectionChanged += (_, _) => collectionEvents++;
        var propertyChanges = 0;
        viewItem.PropertyChanged += (_, _) => propertyChanges++;

        DownloadListSync.Sync(viewItems, [Item(42)]);

        Assert.Equal(0, collectionEvents);
        Assert.Equal(0, propertyChanges);
    }

    /// <summary>Prüft Sync: entfallene Einträge werden entfernt.</summary>
    [Fact]
    public void Sync_RemovedItem_RemovesViewModel()
    {
        var viewItems = new ObservableCollection<DownloadItemViewModel>();
        DownloadListSync.Sync(viewItems, [Item(1), Item(2), Item(3)]);
        var survivor = viewItems[2];

        NotifyCollectionChangedEventArgs? removed = null;
        viewItems.CollectionChanged += (_, e) => removed = e;

        DownloadListSync.Sync(viewItems, [Item(1), Item(3)]);

        Assert.Equal(2, viewItems.Count);
        Assert.Same(survivor, viewItems[1]);
        Assert.Equal(NotifyCollectionChangedAction.Remove, removed?.Action);
        Assert.Equal(2, removed?.OldItems?[0] is DownloadItemViewModel vm ? vm.Item.MediaId : -1);
    }

    /// <summary>Prüft Sync: ein neuer Eintrag wird an seiner Zielposition eingefügt.</summary>
    [Fact]
    public void Sync_AddedItem_InsertsAtTargetPosition()
    {
        var viewItems = new ObservableCollection<DownloadItemViewModel>();
        DownloadListSync.Sync(viewItems, [Item(1), Item(3)]);

        DownloadListSync.Sync(viewItems, [Item(1), Item(2), Item(3)]);

        Assert.Equal(3, viewItems.Count);
        Assert.Equal(2, viewItems[1].Item.MediaId);
    }

    /// <summary>Prüft Sync: geänderte Reihenfolge verschiebt den Eintrag statt ihn neu zu bauen.</summary>
    [Fact]
    public void Sync_ReorderedItems_MovesExistingViewModel()
    {
        var viewItems = new ObservableCollection<DownloadItemViewModel>();
        DownloadListSync.Sync(viewItems, [Item(1), Item(2)]);
        var moved = viewItems[0];

        DownloadListSync.Sync(viewItems, [Item(2), Item(1)]);

        Assert.Equal(2, viewItems.Count);
        Assert.Same(moved, viewItems[1]);
        Assert.Equal(2, viewItems[0].Item.MediaId);
    }

    /// <summary>Prüft Sync: Identität greift über MediaId+MediaType, wenn die EntryId abweicht.</summary>
    [Fact]
    public void Sync_DifferentEntryId_SameMedia_MatchesInPlace()
    {
        var viewItems = new ObservableCollection<DownloadItemViewModel>();
        DownloadListSync.Sync(viewItems, [Item(42, entryId: 7)]);
        var viewItem = viewItems[0];

        var collectionEvents = 0;
        viewItems.CollectionChanged += (_, _) => collectionEvents++;

        var updated = Item(42, entryId: 9) with { DownloadedBytes = 10, TotalBytes = 100 };
        DownloadListSync.Sync(viewItems, [updated]);

        Assert.Equal(0, collectionEvents);
        Assert.Same(viewItem, viewItems[0]);
        Assert.Equal(9, viewItem.Item.EntryId);
    }

    /// <summary>Prüft Update: ein unveränderter Eintrag meldet keine Änderung.</summary>
    [Fact]
    public void ViewModel_Update_UnchangedItem_ReturnsFalse()
    {
        var viewItem = new DownloadItemViewModel(Item(42));

        Assert.False(viewItem.Update(Item(42)));
    }

    /// <summary>Prüft Update: Zustandswechsel meldet die Aktions- und Anzeige-Properties.</summary>
    [Fact]
    public void ViewModel_Update_StateChange_NotifiesActionProperties()
    {
        var viewItem = new DownloadItemViewModel(Item(42));
        var propertyChanges = new List<string?>();
        viewItem.PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);

        Assert.True(viewItem.Update(Item(42, state: DownloadState.Completed)));

        Assert.Equal("Fertig", viewItem.StateText);
        Assert.False(viewItem.CanCancel);
        Assert.Contains(nameof(DownloadItemViewModel.StateText), propertyChanges);
        Assert.Contains(nameof(DownloadItemViewModel.CanCancel), propertyChanges);
        Assert.Contains(nameof(DownloadItemViewModel.ShowProgress), propertyChanges);
    }

    /// <summary>Prüft das ViewModel: Anzeige-Properties werden an den Eintrag delegiert.</summary>
    [Fact]
    public void ViewModel_DelegatesDisplayProperties()
    {
        var viewItem = new DownloadItemViewModel(
            Item(5, "tvshowepisode", DownloadState.Queued) with { Title = "Folge" });

        Assert.Equal("Folge", viewItem.Title);
        Assert.Equal("Episode", viewItem.MediaTypeText);
        Assert.Equal("Wartend", viewItem.StateText);
        Assert.True(viewItem.CanCancel);
        Assert.False(viewItem.ShowProgress);
    }
}
