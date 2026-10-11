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

    /// <summary>Prüft MediaDetailsPage: fordert die eigene Identität erneut
    /// an, wenn der globale Detail-Slot inzwischen ein anderes Medium
    /// beschreibt (Rückkehr von einer geschobenen Episoden-Detailseite —
    /// ohne Wiederanfrage bliebe die Serienansicht leer/schwarz).</summary>
    [Fact]
    public void MediaDetailsPage_Reappearing_RequestsOwnIdentityAfterForeignDetails()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml.cs");

        var onAppearing = MethodBody(code, "protected override void OnAppearing()");
        // Identitätsvergleich gegen den eigenen Routen-Parameter …
        Assert.Contains("state.MediaType", onAppearing, StringComparison.Ordinal);
        Assert.Contains("state.MediaId", onAppearing, StringComparison.Ordinal);
        Assert.Contains("mediaId", onAppearing, StringComparison.Ordinal);
        // … und erneute Anforderung bei abweichender Identität.
        Assert.Contains("!requested || !identityMatches", onAppearing, StringComparison.Ordinal);
        Assert.Contains("MediaDetailsRequestedMessage", onAppearing, StringComparison.Ordinal);
    }

    /// <summary>Extrahiert den Methodenrumpf einer Signatur aus dem Quelltext.</summary>
    /// <param name="code">Der Quelltext der Datei.</param>
    /// <param name="signature">Die Methodensignatur bis zur öffnenden Klammer.</param>
    /// <returns>Der Methodenrumpf inklusive Signatur.</returns>
    private static string MethodBody(string code, string signature)
    {
        var start = code.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Methode '{signature}' nicht gefunden.");
        var open = code.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < code.Length; i++)
        {
            if (code[i] == '{') depth++;
            else if (code[i] == '}' && --depth == 0)
            {
                return code[start..(i + 1)];
            }
        }

        throw new InvalidOperationException($"Rumpf von '{signature}' nicht gefunden.");
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

    /// <summary>Prüft MediaDetailsPage: blendet das Angesehen-Badge nur bei gesehenen Episoden ein.</summary>
    [Fact]
    public void MediaDetailsPage_EpisodeWatchedBadge_BindsIsWatched()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");

        // Das Badge gehört zum Episoden-Template (nach der Item-Auswahl
        // der Episodenliste, vor dem Kollektions-Template).
        var episodeTemplate = xaml.IndexOf("x:DataType=\"mvu:EpisodeItem\"", StringComparison.Ordinal);
        var badgeIndex = xaml.IndexOf("AutomationId=\"WatchedIndicator\"", StringComparison.Ordinal);
        var collectionTemplate = xaml.IndexOf("x:DataType=\"mvu:CollectionMovieItem\"", StringComparison.Ordinal);
        Assert.True(episodeTemplate >= 0);
        Assert.True(badgeIndex > episodeTemplate);
        Assert.True(collectionTemplate > badgeIndex);
        // Sichtbarkeit ausschließlich über den Gesehen-Zeitpunkt
        // (IsWatched = WatchedAt.HasValue) — ungesehene Episoden ohne Badge.
        Assert.Contains("IsVisible=\"{Binding IsWatched}\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft MediaDetailsPage: bildet das Badge nach dem Web-Vorbild (rund, dunkel, Auge in Primärfarbe, oben links).</summary>
    [Fact]
    public void MediaDetailsPage_EpisodeWatchedBadge_FollowsWebDesign()
    {
        var xaml = Read("VideoPlayer.Maui", "Pages", "MediaDetailsPage.xaml");

        // Pill-Form (Web: border-radius 999px), dunkler transluzenter
        // Grund und feiner heller Rand über die Badge-Tokens.
        Assert.Contains("CornerRadius=\"999\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VpBadgeBg", xaml, StringComparison.Ordinal);
        Assert.Contains("VpBadgeBorder", xaml, StringComparison.Ordinal);
        // Position oben links über dem Poster (Web: -image-left).
        var badgeIndex = xaml.IndexOf("AutomationId=\"WatchedIndicator\"", StringComparison.Ordinal);
        var badgeSection = xaml.Substring(badgeIndex, xaml.IndexOf("</Border>", badgeIndex, StringComparison.Ordinal) - badgeIndex);
        Assert.Contains("HorizontalOptions=\"Start\"", badgeSection, StringComparison.Ordinal);
        Assert.Contains("VerticalOptions=\"Start\"", badgeSection, StringComparison.Ordinal);
        // Auge in weicher Primärfarbe: Kontur-Pfad des Web-SVG.
        Assert.Contains("VpPrimarySoft", badgeSection, StringComparison.Ordinal);
        Assert.Contains("M2 12C4 7 8 4 12 4", badgeSection, StringComparison.Ordinal);
    }

    /// <summary>Prüft EpisodeItem: trägt den Gesehen-Zeitpunkt als Sichtbarkeitsquelle des Badges.</summary>
    [Fact]
    public void EpisodeItem_CarriesWatchedState()
    {
        var view = Read("VideoPlayer.Mvu", "MediaDetailsView.cs");

        Assert.Contains("DateTime? WatchedAt", view, StringComparison.Ordinal);
        Assert.Contains("WatchedAt.HasValue", view, StringComparison.Ordinal);
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
