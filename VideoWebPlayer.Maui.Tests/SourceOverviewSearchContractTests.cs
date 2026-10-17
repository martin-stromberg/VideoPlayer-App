using System.Xml.Linq;
using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests der Sucheingabe der <c>SourceOverviewPage</c> (Issue #49,
/// Schritt 2): die unbestätigte Eingabe im <c>MediaSearchBar</c> muss
/// beliebige Modell-Ticks überstehen — <c>UpdateUi</c> darf den Feldinhalt
/// nur dann mit dem abgeschickten <c>SourceBrowserState.SearchText</c>
/// überschreiben, wenn sich dieser Modellwert geändert hat. Die
/// MAUI-Schicht ist im Testprojekt nicht instanziierbar; die Verdrahtung
/// wird daher quelltextseitig gepinnt (Muster:
/// <see cref="PageLifecycleContractTests"/>).
/// </summary>
public class SourceOverviewSearchContractTests
{
    private static readonly string Root = RepositoryRoot.Find();
    private static readonly XNamespace MauiNs = "http://schemas.microsoft.com/dotnet/2021/maui";
    private static readonly XNamespace XamlNs = "http://schemas.microsoft.com/winfx/2009/xaml";

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Pinnt den Spiegel-Merker: die Seite hält den zuletzt ins Feld gespiegelten Modell-Suchtext.</summary>
    [Fact]
    public void SourceOverviewPage_KeepsMirroredSearchText()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "SourceOverviewPage.xaml.cs");

        Assert.Contains("private string? mirroredSearchText", code, StringComparison.Ordinal);
    }

    /// <summary>Pinnt den Änderungsguard: <c>UpdateUi</c> schreibt das Suchfeld nur bei geändertem Modell-Suchtext.</summary>
    [Fact]
    public void UpdateUi_WritesSearchText_OnlyWhenModelSearchTextChanged()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "SourceOverviewPage.xaml.cs");
        var body = MethodBody(code, "private void UpdateUi(");

        // Der Schreibzugriff muss hinter einem Guard liegen, der den
        // gemerkten Spiegelwert mit dem Modellwert vergleicht — nicht
        // hinter einem bloßen Ungleichheitsvergleich mit dem Feldinhalt.
        var guard = body.IndexOf("mirroredSearchText, browser.SearchText", StringComparison.Ordinal);
        var write = body.IndexOf("MediaSearchBar.Text = browser.SearchText", StringComparison.Ordinal);
        Assert.True(guard >= 0, "Spiegel-Guard auf den Modell-Suchtext fehlt.");
        Assert.True(write >= 0, "Feld-Spiegelung des Suchtexts fehlt.");
        Assert.True(guard < write, "Die Feld-Spiegelung muss innerhalb des Modell-Änderungsguards stehen.");
        Assert.DoesNotContain("MediaSearchBar.Text, browser.SearchText", body, StringComparison.Ordinal);
    }

    /// <summary>Pinnt, dass das Suchfeld für DevApi/FlaUI erreichbar ist.</summary>
    [Fact]
    public void MediaSearchBar_HasAutomationId()
    {
        var xaml = XDocument.Parse(
            Read("VideoPlayer.Maui", "Pages", "SourceOverviewPage.xaml"));
        var searchBar = xaml.Descendants(MauiNs + "SearchBar")
            .FirstOrDefault(e => (string?)e.Attribute(XamlNs + "Name") == "MediaSearchBar");

        Assert.NotNull(searchBar);
        Assert.Equal("MediaSearchBar", (string?)searchBar!.Attribute("AutomationId"));
        Assert.Equal("OnSearchButtonPressed", (string?)searchBar.Attribute("SearchButtonPressed"));
    }

    /// <summary>Pinnt, dass das Abschicken den Feldinhalt als <c>SourceSearchSubmittedMessage</c> dispatcht.</summary>
    [Fact]
    public void OnSearchButtonPressed_DispatchesEnteredText()
    {
        var code = Read("VideoPlayer.Maui", "Pages", "SourceOverviewPage.xaml.cs");
        var body = MethodBody(code, "private void OnSearchButtonPressed");

        Assert.Contains("SourceSearchSubmittedMessage(MediaSearchBar.Text)", body, StringComparison.Ordinal);
    }

    /// <summary>Extrahiert den Rumpf einer Methode ab der ersten öffnenden Klammer.</summary>
    /// <param name="code">Quelltext der Datei.</param>
    /// <param name="signature">Eindeutige Signaturmarke der Methode.</param>
    /// <returns>Methodenrumpf inklusive Klammern.</returns>
    private static string MethodBody(string code, string signature)
    {
        var start = code.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Methode '{signature}' nicht gefunden.");
        var open = code.IndexOf('{', start);
        Assert.True(open >= 0, $"Rumpf von '{signature}' nicht gefunden.");
        var depth = 0;
        for (var i = open; i < code.Length; i++)
        {
            if (code[i] == '{')
            {
                depth++;
            }
            else if (code[i] == '}' && --depth == 0)
            {
                return code.Substring(open, i - open + 1);
            }
        }

        return code[open..];
    }
}
