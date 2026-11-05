using System.Text.RegularExpressions;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Contract-Tests gegen tvOS-Kompilierfehler der Klasse „Member eines
/// abgeleiteten Typs auf einem Basistyp aufgerufen" (CS1061): Das
/// tvOS-Projekt degradiert unter Windows zur leeren Bibliothek
/// (<c>EnableDefaultCompileItems=false</c>), darum deckt der lokale
/// Build solche Fehler nicht ab — diese Quelltext-Prüfung macht sie
/// plattformunabhängig sichtbar (Muster: <see cref="TvOsConnectionContractTests"/>).
/// </summary>
public class TvOsCompileContractTests
{
    private static readonly string Root = RepositoryRoot.Find();

    private static IEnumerable<string> TvOsSourceFiles()
    {
        var directory = Path.Combine(Root, "VideoPlayer.TvOS");
        return Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
    }

    /// <summary>
    /// Prüft: auf <c>UIView</c>-deklarierte Felder/Variablen werden keine
    /// <c>UIStackView</c>-Member (<c>Spacing</c>,
    /// <c>AddArrangedSubview</c>) angewandt — die Instanzen müssen mit dem
    /// abgeleiteten Typ deklariert sein.
    /// </summary>
    [Fact]
    public void UiViewDeclarations_DoNotUseStackViewMembers()
    {
        var violations = new List<string>();
        var declaration = new Regex(@"\bUIView\??\s+(\w+)\s*[;=]", RegexOptions.Compiled);

        foreach (var path in TvOsSourceFiles())
        {
            var content = File.ReadAllText(path);
            var fileName = Path.GetFileName(path);
            foreach (Match match in declaration.Matches(content))
            {
                var name = match.Groups[1].Value;
                foreach (var member in new[] { "Spacing", "AddArrangedSubview" })
                {
                    if (content.Contains($"{name}.{member}", StringComparison.Ordinal))
                    {
                        violations.Add($"{fileName}: {name}.{member}");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    /// <summary>
    /// Prüft: <c>AppRoutes.MenuPages</c> ist <c>IReadOnlyList&lt;AppPage&gt;</c>
    /// und besitzt kein <c>IndexOf</c>-Mitglied — Aufrufe kompilieren unter
    /// <c>net10.0-tvos</c> nicht.
    /// </summary>
    [Fact]
    public void MenuPages_IsNotAccessedWithIndexOf()
    {
        var violations = new List<string>();
        foreach (var path in TvOsSourceFiles())
        {
            var content = File.ReadAllText(path);
            if (content.Contains("MenuPages.IndexOf", StringComparison.Ordinal))
            {
                violations.Add(Path.GetFileName(path));
            }
        }

        Assert.Empty(violations);
    }
}
