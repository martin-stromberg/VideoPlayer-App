using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des Weiterschauen-Kontextmenüs (Issue #43, Schritt 2):
/// Long-Press-Verdrahtung des Karussells, das Aktionsmenü der
/// Startseite, die MVU-Effekt-Behandlung und die
/// Developer-API-Auslösung werden über die Quelldateien gepinnt, weil
/// das Testprojekt die MAUI-Targets nicht referenzieren kann.
/// </summary>
public class ContinueWatchingContextMenuContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string ReadFile(params string[] parts)
        => File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Der Karussell-Eintrag trägt neben dem Tap- den Pointer-Recognizer; der 2-s-Timer ist benannt gepinnt.</summary>
    [Fact]
    public void MediaCarousel_ItemCard_HasTapAndPointerRecognizers()
    {
        var xaml = ReadFile("VideoPlayer.Maui", "Controls", "MediaCarousel.xaml");

        Assert.Contains("<TapGestureRecognizer Tapped=\"OnItemTapped\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<PointerGestureRecognizer PointerPressed=\"OnItemPointerPressed\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerReleased=\"OnItemPointerEnded\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PointerExited=\"OnItemPointerEnded\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>Der Code-Behind kapselt die Long-Press-Erkennung: Freigabe-Flag, 2-s-Konstante, Ereignis, Timer-Verwerfen.</summary>
    [Fact]
    public void MediaCarousel_CodeBehind_ImplementsLongPress()
    {
        var source = ReadFile("VideoPlayer.Maui", "Controls", "MediaCarousel.xaml.cs");

        Assert.Contains("LongPressEnabledProperty", source, StringComparison.Ordinal);
        Assert.Contains("event EventHandler<HomeMediaItem>? ItemLongPressed", source, StringComparison.Ordinal);
        Assert.Contains("LongPressDurationMs = 2000", source, StringComparison.Ordinal);
        Assert.Contains("Dispatcher.CreateTimer", source, StringComparison.Ordinal);
        Assert.Contains("CancelLongPress", source, StringComparison.Ordinal);
        Assert.Contains("SendItemLongPressed", source, StringComparison.Ordinal);
        Assert.Contains("if (!LongPressEnabled", source, StringComparison.Ordinal);
    }

    /// <summary>Nur das Weiterschauen-Karussell aktiviert und verdrahtet den Long-Press — Favoriten und Neu im Programm bleiben frei davon.</summary>
    [Fact]
    public void HomePage_LongPress_OnlyWiredOnContinueWatchingCarousel()
    {
        var xaml = ReadFile("VideoPlayer.Maui", "Pages", "HomePage.xaml");

        Assert.Equal(1, xaml.Split("LongPressEnabled=\"True\"").Length - 1);
        Assert.Equal(1, xaml.Split("ItemLongPressed=\"OnContinueWatchingItemLongPressed\"").Length - 1);
        var section = xaml.Split("ContinueWatchingCarousel")[1];
        Assert.Contains("LongPressEnabled=\"True\"", section, StringComparison.Ordinal);
        Assert.Contains("ItemLongPressed=\"OnContinueWatchingItemLongPressed\"", section, StringComparison.Ordinal);
    }

    /// <summary>Die Startseite zeigt das Kontextmenü mit den deutschen Aktionen und dispatcht die beiden Mutations-Nachrichten.</summary>
    [Fact]
    public void HomePage_LongPress_ShowsActionSheet_WithGermanLabels_AndDispatchesMessages()
    {
        var source = ReadFile("VideoPlayer.Maui", "Pages", "HomePage.xaml.cs");

        Assert.Contains("OnContinueWatchingItemLongPressed", source, StringComparison.Ordinal);
        Assert.Contains("DisplayActionSheet", source, StringComparison.Ordinal);
        Assert.Contains("\"Ausblenden\"", source, StringComparison.Ordinal);
        Assert.Contains("\"Überspringen\"", source, StringComparison.Ordinal);
        Assert.Contains("\"Abbrechen\"", source, StringComparison.Ordinal);
        Assert.Contains("ContinueWatchingHideRequestedMessage", source, StringComparison.Ordinal);
        Assert.Contains("ContinueWatchingSkipRequestedMessage", source, StringComparison.Ordinal);
    }

    /// <summary>Der Effekt-Handler behandelt beide Mutations-Effekte und meldet die Cache-Änderung bei Erfolg.</summary>
    [Fact]
    public void ShellAppEffectHandler_HandlesHideAndSkipEffects()
    {
        var source = ReadFile("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("case HideContinueWatchingEntryEffect hide:", source, StringComparison.Ordinal);
        Assert.Contains("case SkipContinueWatchingEntryEffect skip:", source, StringComparison.Ordinal);
        Assert.Contains("ContinueWatchingMutationService", source, StringComparison.Ordinal);
        Assert.Contains("MutateContinueWatchingAsync", source, StringComparison.Ordinal);
        Assert.Contains("CachedListsChangedMessage", source, StringComparison.Ordinal);
    }

    /// <summary>Der Mutationsdienst ruft die bestehenden API-Endpunkte und den Sync-Pfad auf und ist registriert.</summary>
    [Fact]
    public void ContinueWatchingMutationService_UsesApiEndpoints_AndIsRegistered()
    {
        var service = ReadFile("VideoPlayer.Mvu", "ContinueWatchingMutationService.cs");

        Assert.Contains("HideContinueWatchingEntryAsync", service, StringComparison.Ordinal);
        Assert.Contains("SkipContinueWatchingEntryAsync", service, StringComparison.Ordinal);
        Assert.Contains("SyncContinueWatchingAsync", service, StringComparison.Ordinal);
        Assert.Contains("ServerSyncService.ContinueWatchingCacheKey", service, StringComparison.Ordinal);

        var mauiProgram = ReadFile("VideoPlayer.Maui", "MauiProgram.cs");
        Assert.Contains("AddSingleton<ContinueWatchingMutationService>", mauiProgram, StringComparison.Ordinal);
    }

    /// <summary>Die Developer-API löst den Long-Press über das optionale gesture-Feld und die SendItemLongPressed-Konvention aus.</summary>
    [Fact]
    public void DevApi_Invoke_SupportsLongPressGesture()
    {
        var service = ReadFile("msTools.DeveloperApi.Maui", "MauiDevApiService.cs");
        var tree = ReadFile("msTools.DeveloperApi.Maui", "DevApiVisualTree.cs");

        Assert.Contains("public string? Gesture { get; set; }", service, StringComparison.Ordinal);
        Assert.Contains("body.Gesture", service, StringComparison.Ordinal);
        Assert.Contains("\"longpress\"", tree, StringComparison.Ordinal);
        Assert.Contains("\"SendItemLongPressed\"", tree, StringComparison.Ordinal);
        Assert.Contains("types: [typeof(int)]", tree, StringComparison.Ordinal);
    }
}
