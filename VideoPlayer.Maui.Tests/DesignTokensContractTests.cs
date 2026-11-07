using System.Text.RegularExpressions;
using System.Xml.Linq;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests für die Design-Token-Umstellung (Schritt 1 des Projekts
/// „Design-Angleichung an Webanwendung"): pinnen die Web-Design-Tokens in
/// `Colors.xaml`, die Radius-Vorgaben (Buttons 4, Karten 8, Chips/Badges/
/// Icon-Buttons pill-förmig) und verbieten hartcodierte Farben in den
/// Seiten, Steuerelementen und Code-Behind-Dateien von `VideoPlayer.Maui`.
/// Die MAUI-Schicht kann im Testprojekt nicht instanziiert werden — die
/// Prüfung erfolgt über die Quelldateien (Muster: <see cref="SourcesContractTests"/>).
/// </summary>
public class DesignTokensContractTests
{
    private static readonly string Root = RepositoryRoot.Find();
    private static readonly XNamespace MauiNs = "http://schemas.microsoft.com/dotnet/2021/maui";
    private static readonly XNamespace XNs = "http://schemas.microsoft.com/winfx/2009/xaml";

    // Web-Design-Tokens aus web-design-recherche.md (--vp-* CSS-Variablen).
    // Die rgba-Rahmenfarben liegen als ARGB-Hex vor (α 0,1 bzw. 0,42).
    private static readonly (string Key, string Value)[] WebTokens =
    [
        ("VpBg", "#131313"),
        ("VpBgDeep", "#0e0e0e"),
        ("VpSurfaceLow", "#1c1b1b"),
        ("VpSurface", "#201f1f"),
        ("VpSurfaceHigh", "#2a2a2a"),
        ("VpSurfaceBright", "#393939"),
        ("VpText", "#e5e2e1"),
        ("VpTextMuted", "#bda9a6"),
        ("VpTextSoft", "#e9bcb6"),
        ("VpBorder", "#1AFFFFFF"),
        ("VpBorderStrong", "#6BFFB4AA"),
        ("VpPrimary", "#e50914"),
        ("VpPrimaryHover", "#c0000c"),
        ("VpPrimarySoft", "#ffb4aa"),
        ("VpSecondary", "#00a8e8"),
        ("VpSecondarySoft", "#83cfff"),
        ("VpDanger", "#ffb4ab"),
    ];

    private static readonly string[] UiXamlFiles =
        Directory.GetFiles(Path.Combine(Root, "VideoPlayer.Maui", "Pages"), "*.xaml")
            .Concat(Directory.GetFiles(Path.Combine(Root, "VideoPlayer.Maui", "Controls"), "*.xaml"))
            .Concat([Path.Combine(Root, "VideoPlayer.Maui", "AppShell.xaml")])
            .ToArray();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    private static Dictionary<string, string> LoadColorResources()
    {
        var document = XDocument.Parse(Read("VideoPlayer.Maui", "Resources", "Styles", "Colors.xaml"));
        return document.Root!
            .Elements(MauiNs + "Color")
            .ToDictionary(
                e => (string)e.Attribute(XNs + "Key")!,
                e => e.Value.Trim(),
                StringComparer.Ordinal);
    }

    /// <summary>Prüft Colors.xaml: alle dokumentierten Web-Design-Tokens sind mit exaktem Wert abgebildet.</summary>
    [Fact]
    public void Colors_DefinesAllWebTokens()
    {
        var colors = LoadColorResources();

        foreach (var (key, value) in WebTokens)
        {
            Assert.True(colors.TryGetValue(key, out var actual), $"Token {key} fehlt in Colors.xaml");
            Assert.Equal(value, actual, ignoreCase: true);
        }
    }

    /// <summary>Prüft Colors.xaml: die app-eigenen Hilfstokens für Overlay und Schatten sind vorhanden.</summary>
    [Fact]
    public void Colors_DefinesOverlayAndShadowTokens()
    {
        var colors = LoadColorResources();
        var xaml = Read("VideoPlayer.Maui", "Resources", "Styles", "Colors.xaml");

        Assert.Equal("#80000000", colors["VpOverlay"], ignoreCase: true);
        Assert.Equal("#000000", colors["VpShadow"], ignoreCase: true);
        Assert.Contains("x:Key=\"VpShadowBrush\"", xaml, StringComparison.Ordinal);
    }

    /// <summary>Prüft Colors.xaml: die Badge-Tokens des Angesehen-Symbols sind mit den Web-Werten abgebildet.</summary>
    [Fact]
    public void Colors_DefinesWatchedBadgeTokens()
    {
        var colors = LoadColorResources();

        // Web .watched-indicator: rgba(0,0,0,.68) Grund,
        // rgba(255,255,255,.28) Rand — als ARGB-Hex.
        Assert.Equal("#AD000000", colors["VpBadgeBg"], ignoreCase: true);
        Assert.Equal("#47FFFFFF", colors["VpBadgeBorder"], ignoreCase: true);
    }

    /// <summary>Prüft Colors.xaml: die alte MAUI-Template-Palette ist vollständig entfernt.</summary>
    [Fact]
    public void Colors_ContainsNoTemplatePalette()
    {
        var xaml = Read("VideoPlayer.Maui", "Resources", "Styles", "Colors.xaml");
        var keys = Regex.Matches(xaml, @"x:Key=""([^""]+)""").Select(m => m.Groups[1].Value).ToList();

        foreach (var key in keys)
        {
            Assert.Matches(@"^Vp[A-Z]", key);
            Assert.DoesNotMatch(@"^(Gray\d+|OffBlack|Magenta|MidnightBlue|Primary|Secondary|Tertiary|White|Black)$", key);
        }
    }

    /// <summary>Prüft Seiten, Steuerelemente und Shell: keine hartcodierten Hexfarben oder benannten Farbliterale mehr.</summary>
    [Fact]
    public void PagesAndControls_ContainNoHardcodedColors()
    {
        // Gängige XAML-Farbnamen, die nicht mehr als Literal auftauchen
        // dürfen (ausgenommen Transparent — das ist ein Zustand, keine Farbe).
        var namedColors = new Regex(
            @"=""(Red|Green|Blue|White|Black|Gray|Grey|Yellow|Orange|Purple|Pink|Brown|Magenta|Cyan|Lime|Teal|Navy|Maroon|Olive|Silver|Gold|Aqua|Fuchsia|Salmon|Coral|Khaki|Indigo|Violet|Turquoise|Beige|Tan|Chocolate|Crimson|Firebrick|Goldenrod|HotPink|Tomato|SkyBlue|DeepSkyBlue|RoyalBlue|ForestGreen|OrangeRed|LightBlue|LightGreen|LightGray|DarkGray|DarkRed|DarkGreen|DarkBlue|SlateGray|SlateGrey|SteelBlue|AliceBlue)""",
            RegexOptions.IgnoreCase);
        var hexColor = new Regex(@"#[0-9a-fA-F]{3,8}\b");

        foreach (var file in UiXamlFiles)
        {
            var xaml = File.ReadAllText(file);
            Assert.False(hexColor.IsMatch(xaml), $"Hexfarbe in {Path.GetFileName(file)} gefunden");
            Assert.False(namedColors.IsMatch(xaml), $"Farbliteral in {Path.GetFileName(file)} gefunden");
        }
    }

    /// <summary>Prüft Code-Behind von Seiten/Steuerelementen/Shell: keine hartcodierten Farben.</summary>
    [Fact]
    public void CodeBehind_ContainsNoHardcodedColors()
    {
        var files = Directory
            .GetFiles(Path.Combine(Root, "VideoPlayer.Maui", "Pages"), "*.xaml.cs")
            .Concat(Directory.GetFiles(Path.Combine(Root, "VideoPlayer.Maui", "Controls"), "*.cs"))
            .Concat([Path.Combine(Root, "VideoPlayer.Maui", "AppShell.xaml.cs")]);

        var forbidden = new Regex(@"Colors\.|Color\.From(Argb|Rgb|Rgba|Hex|Int|Uint)|new Color\(");

        foreach (var file in files)
        {
            var code = File.ReadAllText(file);
            Assert.False(forbidden.IsMatch(code), $"Hartcodierte Farbe in {Path.GetFileName(file)} gefunden");
        }
    }

    /// <summary>Prüft Styles.xaml: Buttons haben den modernen Radius 10, Karten 14, Poster 12.</summary>
    [Fact]
    public void Styles_PinsButtonAndCardRadius()
    {
        var document = XDocument.Parse(Read("VideoPlayer.Maui", "Resources", "Styles", "Styles.xaml"));

        // Impliziter Button-Style (ohne x:Key): CornerRadius 10.
        var buttonStyle = document.Root!
            .Elements(MauiNs + "Style")
            .Single(e =>
                (string?)e.Attribute("TargetType") == "Button" &&
                e.Attribute(XNs + "Key") is null);
        var radius = buttonStyle
            .Elements(MauiNs + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "CornerRadius");
        Assert.Equal("10", (string?)radius.Attribute("Value"));

        // Kartenradius 14 steckt im zentralen Card-Stil; die Karten-
        // Templates der Seiten verwenden ihn einheitlich.
        var cardStyle = document.Root!
            .Elements(MauiNs + "Style")
            .Single(e => (string?)e.Attribute(XNs + "Key") == "Card");
        var cardShape = cardStyle
            .Elements(MauiNs + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "StrokeShape");
        Assert.Equal("RoundRectangle 14", (string?)cardShape.Attribute("Value"));

        foreach (var file in new[] { "DownloadsPage.xaml", "FavoritesPage.xaml", "MediaDetailsPage.xaml", "ProfilePage.xaml" })
        {
            Assert.Contains(
                "{StaticResource Card}",
                Read("VideoPlayer.Maui", "Pages", file),
                StringComparison.Ordinal);
        }

        Assert.Contains(
            "CornerRadius=\"12\"",
            Read("VideoPlayer.Maui", "Controls", "MediaCarousel.xaml"),
            StringComparison.Ordinal);
    }

    /// <summary>Prüft: Chips, Badges und runde Icon-Schaltflächen sind pill-förmig (999).</summary>
    [Fact]
    public void Styles_PinsPillShapes()
    {
        var styles = Read("VideoPlayer.Maui", "Resources", "Styles", "Styles.xaml");
        var sourceOverview = Read("VideoPlayer.Maui", "Pages", "SourceOverviewPage.xaml");
        var carousel = Read("VideoPlayer.Maui", "Controls", "MediaCarousel.xaml");

        // IconButton-Style: pill-förmige Icon-Schaltflächen.
        var document = XDocument.Parse(styles);
        var iconButton = document.Root!
            .Elements(MauiNs + "Style")
            .Single(e => (string?)e.Attribute(XNs + "Key") == "IconButton");
        var radius = iconButton
            .Elements(MauiNs + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "CornerRadius");
        Assert.Equal("999", (string?)radius.Attribute("Value"));

        // Chip-Stil (Genre-Filter) und Platzhalter-Badge sind pill-förmig.
        var chipStyle = document.Root!
            .Elements(MauiNs + "Style")
            .Single(e => (string?)e.Attribute(XNs + "Key") == "Chip");
        var chipShape = chipStyle
            .Elements(MauiNs + "Setter")
            .Single(s => (string?)s.Attribute("Property") == "StrokeShape");
        Assert.Equal("RoundRectangle 999", (string?)chipShape.Attribute("Value"));
        Assert.Contains("{StaticResource Chip}", sourceOverview, StringComparison.Ordinal);
        Assert.Contains("CornerRadius=\"999\"", carousel, StringComparison.Ordinal);
    }

    /// <summary>Prüft Styles.xaml: Seiten- und Shell-Hintergründe nutzen die Web-Schwarztöne.</summary>
    [Fact]
    public void Styles_PinsWebBackgrounds()
    {
        var styles = Read("VideoPlayer.Maui", "Resources", "Styles", "Styles.xaml");

        Assert.Contains(
            "<Setter Property=\"BackgroundColor\" Value=\"{StaticResource VpBg}\" />",
            styles,
            StringComparison.Ordinal);
        Assert.Contains(
            "<Setter Property=\"Shell.BackgroundColor\" Value=\"{StaticResource VpBg}\" />",
            styles,
            StringComparison.Ordinal);
        Assert.Contains(
            "<Setter Property=\"Shell.FlyoutBackgroundColor\" Value=\"{StaticResource VpBgDeep}\" />",
            styles,
            StringComparison.Ordinal);
    }

    /// <summary>Prüft App.xaml.cs: die App ist fest auf das dunkle Web-Erscheinungsbild gepinnt.</summary>
    [Fact]
    public void App_ForcesDarkTheme()
    {
        var code = Read("VideoPlayer.Maui", "App.xaml.cs");

        Assert.Contains("UserAppTheme = AppTheme.Dark", code, StringComparison.Ordinal);
    }

    /// <summary>Prüft alle XAML-Dateien der App: Trigger-Sammlungen hängen
    /// an einem Element des passenden TargetType — ein <c>&lt;Label.Triggers&gt;</c>
    /// als Geschwister unter einer <c>Border</c> würde zur Laufzeit in
    /// <c>AttachTo</c> mit „bindable not an instance of AssociatedType" knallen.</summary>
    [Fact]
    public void Xaml_TriggersAttachToMatchingElementType()
    {
        var baseTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Element", "VisualElement", "BindableObject", "NavigableElement",
            "InputView", "View", "Layout"
        };

        foreach (var file in UiXamlFiles)
        {
            var document = XDocument.Load(file);
            foreach (var triggers in document.Descendants()
                         .Where(e => e.Name.LocalName.EndsWith(".Triggers", StringComparison.Ordinal)))
            {
                var qualifier = triggers.Name.LocalName[..^".Triggers".Length];
                if (baseTypes.Contains(qualifier))
                {
                    continue;
                }

                var parentType = triggers.Parent!.Name.LocalName;
                Assert.True(
                    string.Equals(qualifier, parentType, StringComparison.Ordinal),
                    $"{Path.GetFileName(file)}: <{qualifier}.Triggers> hängt an <{parentType}> statt an <{qualifier}>");
            }
        }
    }

    /// <summary>Prüft alle XAML-Dateien der App: jeder StaticResource-Verweis löst auf einen definierten Schlüssel auf.</summary>
    [Fact]
    public void Xaml_UsesOnlyKnownResourceKeys()
    {
        var mauiDir = Path.Combine(Root, "VideoPlayer.Maui");
        var xamlFiles = Directory
            .GetFiles(mauiDir, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var definedKeys = xamlFiles
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"x:Key=""([^""]+)"""))
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        var referencedKeys = xamlFiles
            .SelectMany(f => Regex.Matches(
                File.ReadAllText(f),
                @"\{StaticResource ([^} ,]+)\}"))
            .Select(m => (m.Groups[1].Value, m.Index))
            .ToList();

        Assert.NotEmpty(referencedKeys);
        foreach (var (key, _) in referencedKeys)
        {
            Assert.True(definedKeys.Contains(key), $"Unbekannter StaticResource-Schlüssel: {key}");
        }
    }
}
