using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// MVU-Übergänge des Weiterschauen-Kontextmenüs (Issue #43, Schritt 2):
/// „Ausblenden" und „Überspringen" erzeugen je den zugehörigen
/// Mutations-Effekt mit der Server-Typkonvention des Eintrags; Guards
/// (Id, Medientyp, Platzhalter) lassen die Anforderung wirkungslos.
/// </summary>
public class AppUpdateTests_ContinueWatchingActions
{
    private static AppModel Connected() => new()
    {
        ConnectionState = ConnectionState.Connected,
        IsStartupRunning = false
    };

    private static HomeMediaItem Item(
        string mediaType,
        long id,
        string title = "Titel",
        bool isPlaceholder = false) => new()
        {
            Id = id,
            MediaType = mediaType,
            Title = title,
            IsPlaceholder = isPlaceholder
        };

    /// <summary>Prüft „Ausblenden": ein verwertbarer Eintrag erzeugt den Hide-Effekt mit Typ und Id des Eintrags.</summary>
    [Fact]
    public void ContinueWatchingHideRequested_ValidItem_ProducesHideEffect()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingHideRequestedMessage(Item("movie", 42, "Matrix")));

        var effect = Assert.IsType<HideContinueWatchingEntryEffect>(
            Assert.Single(transition.Effects));
        Assert.Equal("movie", effect.MediaType);
        Assert.Equal(42, effect.MediaId);
    }

    /// <summary>Prüft „Überspringen": ein verwertbarer Eintrag erzeugt den Skip-Effekt mit Typ und Id des Eintrags.</summary>
    [Fact]
    public void ContinueWatchingSkipRequested_ValidItem_ProducesSkipEffect()
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingSkipRequestedMessage(Item("episode", 31, "Pilot")));

        var effect = Assert.IsType<SkipContinueWatchingEntryEffect>(
            Assert.Single(transition.Effects));
        Assert.Equal("episode", effect.MediaType);
        Assert.Equal(31, effect.MediaId);
    }

    /// <summary>Prüft die Server-Typkonvention: der Listen-Medientyp geht unverändert in den Effekt.</summary>
    /// <param name="mediaType">Server-Typ des Weiterschauen-Eintrags.</param>
    [Theory]
    [InlineData("show")]
    [InlineData("season")]
    [InlineData("collection")]
    [InlineData("tvshowepisode")]
    public void ContinueWatchingHideRequested_ServerType_PassedThroughUnchanged(string mediaType)
    {
        var transition = AppUpdate.Update(
            Connected(),
            new ContinueWatchingHideRequestedMessage(Item(mediaType, 12)));

        var effect = Assert.IsType<HideContinueWatchingEntryEffect>(
            Assert.Single(transition.Effects));
        Assert.Equal(mediaType, effect.MediaType);
    }

    /// <summary>Prüft die Guards der Ausblenden-Anforderung: ohne verwertbares Medium bleibt der Übergang wirkungslos.</summary>
    /// <param name="item">Der zu prüfende Eintrag.</param>
    [Theory]
    [MemberData(nameof(NonSelectableItems))]
    public void ContinueWatchingHideRequested_NonSelectable_Unchanged(HomeMediaItem item)
    {
        var model = Connected();

        var transition = AppUpdate.Update(model, new ContinueWatchingHideRequestedMessage(item));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Prüft die Guards der Überspringen-Anforderung: ohne verwertbares Medium bleibt der Übergang wirkungslos.</summary>
    /// <param name="item">Der zu prüfende Eintrag.</param>
    [Theory]
    [MemberData(nameof(NonSelectableItems))]
    public void ContinueWatchingSkipRequested_NonSelectable_Unchanged(HomeMediaItem item)
    {
        var model = Connected();

        var transition = AppUpdate.Update(model, new ContinueWatchingSkipRequestedMessage(item));

        Assert.Same(model, transition.Model);
        Assert.Empty(transition.Effects);
    }

    /// <summary>Nicht verwertbare Einträge: Id 0, leerer Medientyp, Offline-Platzhalter.</summary>
    /// <returns>Die Testfälle der Theory.</returns>
    public static IEnumerable<object[]> NonSelectableItems()
    {
        yield return [Item("movie", 0)];
        yield return [Item("", 12)];
        yield return [Item("episode", 31, isPlaceholder: true)];
    }
}
