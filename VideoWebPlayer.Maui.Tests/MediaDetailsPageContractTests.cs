using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Detailseite (Schritt 6): vollbreiter Kopfbereich
/// mit Hintergrundgrafik nach Web-Vorbild (`.tvshow-header`). Die
/// MAUI-Schicht kann im Testprojekt nicht instanziiert werden —
/// verdrahtete Struktur und Zusammenspiel werden über die Quelldateien
/// geprüft (Muster: <see cref="HomePageContractTests"/>).
/// </summary>
public class MediaDetailsPageContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Prüft MediaDetailsPage: zeigt einen vollbreiten Kopfbereich mit Bild und geschichteten Gradienten.</summary>
    [Fact]
    public void MediaDetailsPage_FullWidthHeader_WithGradients()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");

        // Vollbreite: kein Seiten-Padding am Wurzel-Grid; der Kopfbereich
        // steht als erstes Element im Scroll-Inhalt.
        var gridIndex = xaml.IndexOf("<Grid RowDefinitions=\"Auto,Auto,*\"", StringComparison.Ordinal);
        Assert.True(gridIndex >= 0);
        var headerIndex = xaml.IndexOf("HeaderSection", StringComparison.Ordinal);
        var seasonIndex = xaml.IndexOf("SeasonSection", StringComparison.Ordinal);
        Assert.True(headerIndex > gridIndex);
        Assert.True(seasonIndex > headerIndex);

        // Cover-Bild + seitlicher Dunkelgradient + Bottom-Fade +
        // Trennlinie über den zentralen Design-Tokens.
        Assert.Contains("AspectFill", xaml, StringComparison.Ordinal);
        Assert.Contains("LinearGradientBrush", xaml, StringComparison.Ordinal);
        Assert.Contains("VpHeroSideStrong", xaml, StringComparison.Ordinal);
        Assert.Contains("VpHeroSideMid", xaml, StringComparison.Ordinal);
        Assert.Contains("VpHeroFadeClear", xaml, StringComparison.Ordinal);
        Assert.Contains("VpBg", xaml, StringComparison.Ordinal);
        Assert.Contains("VpBorder", xaml, StringComparison.Ordinal);
        // Kein alter Poster-Kopf mehr im Kopfbereich.
        Assert.DoesNotContain("ColumnDefinitions=\"140,*\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsPage: überlagert Titel, Meta, Plot und die Schalter auf dem Header.</summary>
    [Fact]
    public void MediaDetailsPage_HeaderOverlaysContentAndActions()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");

        // Inhaltsblock unten links mit Web-Breitenbegrenzung.
        Assert.Contains("MaximumWidthRequest=\"820\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TitleLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("MetaLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("PlotLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("VpPrimarySoft", xaml, StringComparison.Ordinal);
        Assert.Contains("VpTextSoft", xaml, StringComparison.Ordinal);
        // Favoriten-Schalter oben rechts, Aktionen unten rechts auf dem Header.
        Assert.Contains("HorizontalOptions=\"End\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalOptions=\"Start\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FavoriteButton", xaml, StringComparison.Ordinal);
        Assert.Contains("PlayButton", xaml, StringComparison.Ordinal);
        Assert.Contains("DownloadButton", xaml, StringComparison.Ordinal);
        // Bestehende Inhalte unterhalb des Headers bleiben erhalten.
        Assert.Contains("SeasonPicker", xaml, StringComparison.Ordinal);
        Assert.Contains("EpisodesCollection", xaml, StringComparison.Ordinal);
        Assert.Contains("CollectionSection", xaml, StringComparison.Ordinal);
        Assert.Contains("DownloadNoticeLabel", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsPage-Code: bindet die Kopfdaten des Views und koppelt die Höhe an die Breite.</summary>
    [Fact]
    public void MediaDetailsPage_CodeBehind_HeaderBindingAndResponsiveHeight()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml.cs");

        // Kopfzeilen aus dem plattformneutralen View-Modell.
        Assert.Contains("details.HeaderTitle", code, StringComparison.Ordinal);
        Assert.Contains("details.HeaderMeta", code, StringComparison.Ordinal);
        Assert.Contains("details.HeaderPlot", code, StringComparison.Ordinal);
        // Flüchtige Umwandlung byte[] → ImageSource (keine Dateiablage).
        Assert.Contains("state.HeaderImageBytes", code, StringComparison.Ordinal);
        Assert.Contains("ImageSource.FromStream", code, StringComparison.Ordinal);
        Assert.DoesNotContain("WriteAllBytes", code, StringComparison.Ordinal);
        // clamp-Äquivalent der Web-Höhe (360px … 52vw … 620px).
        Assert.Contains("Math.Clamp(Width * 0.52, 360, 620)", code, StringComparison.Ordinal);
        Assert.Contains("SizeChanged", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft ShellAppEffectHandler: führt den Kopfbereichs-Ladeeffekt je Quelle aus.</summary>
    [Fact]
    public void ShellAppEffectHandler_HandlesDetailsHeaderLoadEffect()
    {
        var handler = Read("VideoPlayer.Maui", "Services", "ShellAppEffectHandler.cs");

        Assert.Contains("LoadMediaDetailsHeaderEffect", handler, StringComparison.Ordinal);
        Assert.Contains("GetEpisodeBackgroundImageAsync", handler, StringComparison.Ordinal);
        Assert.Contains("GetPictureAsync", handler, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsHeaderImageLoadedMessage", handler, StringComparison.Ordinal);
        Assert.Contains("authSession.Client", handler, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsView: trägt die plattformneutralen Header-Ableitungen (keine ImageSource im MVU-Core).</summary>
    [Fact]
    public void MediaDetailsView_CarriesHeaderIdentityAndTexts()
    {
        var view = Read("VideoPlayer.Mvu", "MediaDetailsView.cs");

        Assert.Contains("SelectedEpisodeId", view, StringComparison.Ordinal);
        Assert.Contains("HeaderEpisodeId", view, StringComparison.Ordinal);
        Assert.Contains("HeaderPictureId", view, StringComparison.Ordinal);
        Assert.Contains("HeaderTitle", view, StringComparison.Ordinal);
        Assert.Contains("HeaderMeta", view, StringComparison.Ordinal);
        Assert.Contains("HeaderPlot", view, StringComparison.Ordinal);
        Assert.DoesNotContain("ImageSource", view, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsState/AppMessage/AppEffect: tragen den Header-Bildlauf.</summary>
    [Fact]
    public void MvuCore_CarriesHeaderImageStateAndMessages()
    {
        var state = Read("VideoPlayer.Mvu", "MediaDetailsState.cs");
        var messages = Read("VideoPlayer.Mvu", "AppMessage.cs");
        var effects = Read("VideoPlayer.Mvu", "AppEffect.cs");
        var update = Read("VideoPlayer.Mvu", "AppUpdate.cs");

        Assert.Contains("HeaderImageBytes", state, StringComparison.Ordinal);
        Assert.Contains("IsHeaderImageLoading", state, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsHeaderImageLoadedMessage", messages, StringComparison.Ordinal);
        Assert.Contains("LoadMediaDetailsHeaderEffect", effects, StringComparison.Ordinal);
        Assert.Contains("UpdateMediaDetailsHeaderImageLoaded", update, StringComparison.Ordinal);
        Assert.Contains("SelectedEpisodeId", update, StringComparison.Ordinal);
    }

    /// <summary>Prüft VideoPlayerApiClient: ruft den Episoden-Still-Endpunkt über die gemeinsame Sende-Pipeline ab.</summary>
    [Fact]
    public void ApiClient_EpisodeStillEndpoint_UsesSendPipeline()
    {
        var client = Read("VideoPlayer.ApiClient", "VideoPlayerApiClient.cs");

        Assert.Contains("GetEpisodeBackgroundImageAsync", client, StringComparison.Ordinal);
        Assert.Contains("api/episodes/", client, StringComparison.Ordinal);
        Assert.Contains("background-image", client, StringComparison.Ordinal);
        Assert.Contains("SendWithPolicyAsync", client, StringComparison.Ordinal);
    }
}
