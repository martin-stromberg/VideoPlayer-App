using System.Text;
using VideoPlayer.Maui.Tests.Stubs;

namespace VideoPlayer.Maui.Tests;

/// <summary>
/// Konventions-Contract-Tests der Test-Lane-Trennung: erzwingt per
/// Quelltext-Scan, dass das <c>Flaky</c>-Trait nur unmittelbar zusammen
/// mit <c>Category=OsInterface</c> vergeben wird und dass jede
/// E2E-Testklasse das <c>OsInterface</c>-Trait trägt (Muster:
/// <see cref="DevApiContractTests"/>).
/// </summary>
public class OsInterfaceConventionTests
{
    private const string OsInterfaceTrait = "Trait(\"Category\", \"OsInterface\")";
    private const string FlakyTraitPrefix = "Trait(\"Flaky\"";

    private static readonly string Root = RepositoryRoot.Find();

    private static string ThisFile =>
        Path.Combine(Root, "VideoPlayer.Maui.Tests", "OsInterfaceConventionTests.cs");

    private static IEnumerable<string> TestProjectSources()
    {
        foreach (var file in SourceFiles(
                     Path.Combine(Root, "VideoPlayer.Maui.Tests"), "*.cs"))
        {
            yield return file;
        }

        foreach (var file in SourceFiles(
                     Path.Combine(Root, "VideoPlayer.E2E.Tests"), "*.cs"))
        {
            yield return file;
        }
    }

    /// <summary>
    /// Rekursiver Quelltext-Scan: liefert alle <paramref name="pattern"/>-Dateien
    /// unter <paramref name="directory"/>, ohne generierte/kopierte Dateien aus
    /// <c>bin</c>-/<c>obj</c>-Verzeichnissen.
    /// </summary>
    /// <param name="directory">Das zu durchsuchende Projektverzeichnis.</param>
    /// <param name="pattern">Das Dateimuster (z. B. <c>*.cs</c>, <c>*Tests.cs</c>).</param>
    /// <returns>Die gefundenen Dateipfade, relativ zu <paramref name="directory"/> ohne <c>bin</c>/<c>obj</c>.</returns>
    private static IEnumerable<string> SourceFiles(string directory, string pattern)
    {
        return Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
            .Where(file => !IsUnderBuildOutputDirectory(directory, file));
    }

    private static bool IsUnderBuildOutputDirectory(string directory, string file)
    {
        // Nur Segmente des relativen Pfads prüfen: bin/obj-Verzeichnisse
        // oberhalb des Klons dürfen den Scan nicht leerlaufen lassen.
        return Path.GetRelativePath(directory, file)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment is "bin" or "obj");
    }

    /// <summary>
    /// Das <c>Flaky</c>-Trait darf nur im selben zusammenhängenden
    /// Attributblock stehen wie <c>Category=OsInterface</c> — sonst
    /// liefe ein bekannter Flake in der blockierenden Lane. Geprüft
    /// wird zeilenweise auf kommentarbereinigtem Quelltext, damit
    /// auskommentierte Traits nicht mitzählen und ein OsInterface-Trait
    /// an einer anderen Methode der Datei keine unmarkierte
    /// Flaky-Methode deckt. Diese Datei ist vom Scan ausgenommen, weil
    /// sie die Suchmuster als Literale enthält.
    /// </summary>
    [Fact]
    public void FlakyTrait_RequiresOsInterface()
    {
        var violations = new List<string>();
        foreach (var file in TestProjectSources())
        {
            if (string.Equals(file, ThisFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var codeLines = CodeLines(file);
            for (var i = 0; i < codeLines.Count; i++)
            {
                if (!codeLines[i].Contains(FlakyTraitPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var block = AttributeBlock(codeLines, i);
                if (!block.Contains(OsInterfaceTrait, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetFileName(file)}:{i + 1}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Flaky-Trait ohne OsInterface-Markierung im selben Attributblock: "
            + string.Join(", ", violations));
    }

    /// <summary>
    /// Jede <c>*Tests.cs</c>-Datei unter <c>VideoPlayer.E2E.Tests/Tests</c>
    /// trägt das <c>OsInterface</c>-Trait — die gesamte Suite ist
    /// umgebungssensitiv (Prozesse, UIA, Sockets, Dateisystem) und
    /// gehört nicht in die blockierende Lane. Auskommentierte Traits
    /// zählen nicht.
    /// </summary>
    [Fact]
    public void E2eTestClasses_CarryOsInterfaceTrait()
    {
        var testsDir = Path.Combine(Root, "VideoPlayer.E2E.Tests", "Tests");
        var violations = new List<string>();
        foreach (var file in SourceFiles(testsDir, "*Tests.cs"))
        {
            var marked = CodeLines(file).Any(
                line => line.Contains(OsInterfaceTrait, StringComparison.Ordinal));
            if (!marked)
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        Assert.True(violations.Count == 0,
            "E2E-Testklassen ohne OsInterface-Markierung: " + string.Join(", ", violations));
    }

    /// <summary>
    /// Liefert den zusammenhängenden Attributblock um die Zeile
    /// <paramref name="index"/>: alle direkt angrenzenden Zeilen, die
    /// (nach der Kommentarbereinigung) mit <c>[</c> beginnen, bis zur
    /// ersten anderen Codezeile — der deklarierte Member schließt den
    /// Block ab. Leer- und kommentierteilige Zeilen innerhalb des
    /// Blocks sind transparent.
    /// </summary>
    /// <param name="codeLines">Die kommentarbereinigten Quellzeilen der Datei.</param>
    /// <param name="index">Der Zeilenindex des gefundenen Trait-Vorkommens.</param>
    /// <returns>Der zusammenhängende Attributblock um <paramref name="index"/> als ein String.</returns>
    private static string AttributeBlock(IReadOnlyList<string> codeLines, int index)
    {
        var start = index;
        while (start > 0 && IsBlockLine(codeLines[start - 1]))
        {
            start--;
        }

        var end = index;
        while (end + 1 < codeLines.Count && IsBlockLine(codeLines[end + 1]))
        {
            end++;
        }

        return string.Join('\n', codeLines.Skip(start).Take(end - start + 1));
    }

    private static bool IsBlockLine(string codeLine)
    {
        var trimmed = codeLine.TrimStart();
        return trimmed.Length == 0 || trimmed.StartsWith('[');
    }

    /// <summary>
    /// Liest eine Quelldatei zeilenweise und entfernt Zeilen-
    /// (<c>//</c>, <c>///</c>) und Blockkommentare (<c>/* … */</c>),
    /// damit auskommentierte Traits nicht mitgezählt werden.
    /// </summary>
    /// <param name="file">Der Pfad der zu lesenden Quelldatei.</param>
    /// <returns>Die Zeilen der Datei ohne Kommentarbestandteile.</returns>
    private static List<string> CodeLines(string file)
    {
        var lines = new List<string>();
        var inBlockComment = false;
        foreach (var line in File.ReadLines(file))
        {
            lines.Add(StripComments(line, ref inBlockComment));
        }

        return lines;
    }

    private static string StripComments(string line, ref bool inBlockComment)
    {
        var builder = new StringBuilder(line.Length);
        var i = 0;
        while (i < line.Length)
        {
            if (inBlockComment)
            {
                var end = line.IndexOf("*/", i, StringComparison.Ordinal);
                if (end < 0)
                {
                    return builder.ToString();
                }

                inBlockComment = false;
                i = end + 2;
            }
            else if (line[i] == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                break;
            }
            else if (line[i] == '/' && i + 1 < line.Length && line[i + 1] == '*')
            {
                inBlockComment = true;
                i += 2;
            }
            else
            {
                builder.Append(line[i]);
                i++;
            }
        }

        return builder.ToString();
    }
}
