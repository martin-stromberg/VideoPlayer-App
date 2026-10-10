using VideoWebPlayer.Maui.Tests.Stubs;

namespace VideoWebPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests des Seiten-Lebenszyklus (Issue #11): die Seiten, die
/// <c>ModelChanged</c> abonnieren, müssen sich im
/// <c>OnDisappearing</c>-Lebenszyklus wieder abmelden — ein Abo im
/// Konstruktor hält verlassene Seiten-Instanzen am <c>MvuHost</c> fest
/// (Speicherleck) und lässt sie weiter auf Modelländerungen rendern.
/// Die MAUI-Schicht ist im Testprojekt nicht instanziierbar; die
/// Verdrahtung wird daher quelltextseitig gepinnt (Muster:
/// <see cref="PlayerContractTests"/>).
/// </summary>
public class PageLifecycleContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    /// <summary>Prüft HomePage: Abo im Sichtbarkeitszyklus statt im Konstruktor.</summary>
    [Fact]
    public void HomePage_UnsubscribesModelChangedOnDisappearing() =>
        AssertUnsubscribesModelChangedOnDisappearing("HomePage");

    /// <summary>Prüft HomePage: gemarshallte Callbacks laufen nur bei aktivem Abo.</summary>
    [Fact]
    public void HomePage_IgnoresModelChangesAfterDisappearing() =>
        AssertIgnoresModelChangesAfterDisappearing("HomePage");

    /// <summary>Prüft MediaDetailsPage: Abo im Sichtbarkeitszyklus statt im Konstruktor.</summary>
    [Fact]
    public void MediaDetailsPage_UnsubscribesModelChangedOnDisappearing() =>
        AssertUnsubscribesModelChangedOnDisappearing("MediaDetailsPage");

    /// <summary>Prüft MediaDetailsPage: gemarshallte Callbacks laufen nur bei aktivem Abo.</summary>
    [Fact]
    public void MediaDetailsPage_IgnoresModelChangesAfterDisappearing() =>
        AssertIgnoresModelChangesAfterDisappearing("MediaDetailsPage");

    /// <summary>Prüft DownloadsPage: Abo im Sichtbarkeitszyklus statt im Konstruktor.</summary>
    [Fact]
    public void DownloadsPage_UnsubscribesModelChangedOnDisappearing() =>
        AssertUnsubscribesModelChangedOnDisappearing("DownloadsPage");

    /// <summary>Prüft DownloadsPage: gemarshallte Callbacks laufen nur bei aktivem Abo.</summary>
    [Fact]
    public void DownloadsPage_IgnoresModelChangesAfterDisappearing() =>
        AssertIgnoresModelChangesAfterDisappearing("DownloadsPage");

    /// <summary>Prüft FavoritesPage: Abo im Sichtbarkeitszyklus statt im Konstruktor.</summary>
    [Fact]
    public void FavoritesPage_UnsubscribesModelChangedOnDisappearing() =>
        AssertUnsubscribesModelChangedOnDisappearing("FavoritesPage");

    /// <summary>Prüft FavoritesPage: gemarshallte Callbacks laufen nur bei aktivem Abo.</summary>
    [Fact]
    public void FavoritesPage_IgnoresModelChangesAfterDisappearing() =>
        AssertIgnoresModelChangesAfterDisappearing("FavoritesPage");

    /// <summary>Prüft SourceOverviewPage: Abo im Sichtbarkeitszyklus statt im Konstruktor.</summary>
    [Fact]
    public void SourceOverviewPage_UnsubscribesModelChangedOnDisappearing() =>
        AssertUnsubscribesModelChangedOnDisappearing("SourceOverviewPage");

    /// <summary>Prüft SourceOverviewPage: gemarshallte Callbacks laufen nur bei aktivem Abo.</summary>
    [Fact]
    public void SourceOverviewPage_IgnoresModelChangesAfterDisappearing() =>
        AssertIgnoresModelChangesAfterDisappearing("SourceOverviewPage");

    /// <summary>Prüft ProfilePage: Abo im Sichtbarkeitszyklus statt im Konstruktor.</summary>
    [Fact]
    public void ProfilePage_UnsubscribesModelChangedOnDisappearing() =>
        AssertUnsubscribesModelChangedOnDisappearing("ProfilePage");

    /// <summary>Prüft ProfilePage: gemarshallte Callbacks laufen nur bei aktivem Abo.</summary>
    [Fact]
    public void ProfilePage_IgnoresModelChangesAfterDisappearing() =>
        AssertIgnoresModelChangesAfterDisappearing("ProfilePage");

    /// <summary>
    /// Pinnt das Lebenszyklus-Muster: Abmeldung in
    /// <c>OnDisappearing</c>, Anmeldung in <c>OnAppearing</c>, kein Abo im
    /// Konstruktor.
    /// </summary>
    private static void AssertUnsubscribesModelChangedOnDisappearing(string page)
    {
        var code = Read("VideoPlayer.Maui", "Pages", $"{page}.xaml.cs");

        Assert.Contains("ModelChanged -= OnModelChanged",
            MethodBody(code, "protected override void OnDisappearing"),
            StringComparison.Ordinal);
        Assert.Contains("ModelChanged += OnModelChanged",
            MethodBody(code, "protected override void OnAppearing"),
            StringComparison.Ordinal);
        Assert.DoesNotContain("ModelChanged +=",
            MethodBody(code, $"public {page}("),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Pinnt den <c>subscribed</c>-Guard: ein bereits gemarshalter
    /// <c>UpdateUi</c>-Callback darf auf der losgelösten Seite nicht mehr
    /// rendern (Muster: PlayerPage).
    /// </summary>
    private static void AssertIgnoresModelChangesAfterDisappearing(string page)
    {
        var code = Read("VideoPlayer.Maui", "Pages", $"{page}.xaml.cs");

        Assert.Contains("subscribed = false",
            MethodBody(code, "protected override void OnDisappearing"),
            StringComparison.Ordinal);
        Assert.Contains("subscribed",
            MethodBody(code, "private void OnModelChanged"),
            StringComparison.Ordinal);
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
