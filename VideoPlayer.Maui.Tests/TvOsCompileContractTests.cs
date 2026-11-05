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

    /// <summary>
    /// Prüft: benannte Argumente in tvOS-Aufrufen, die keinem positionalen
    /// Record-Konstruktorparameter exakt entsprechen, aber case-insensitiv
    /// einem solchen gleichen — C#-Named-Arguments sind case-sensitiv, ein
    /// abweichender Fall bricht mit CS1739 unter <c>net10.0-tvos</c> (Fund:
    /// <c>isContinueWatching:</c> statt <c>IsContinueWatching:</c> an
    /// <c>HomeSectionDescriptor</c>). Die Referenzmenge bilden die
    /// positionalen Record-Parameter des tvOS-Projekts und seiner
    /// Projektreferenzen.
    /// </summary>
    [Fact]
    public void NamedArguments_MatchRecordParameterCase()
    {
        var parameterNames = RecordConstructorParameterNames();
        var caseInsensitive = parameterNames
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var argumentName = new Regex(@"(?:\(|,)\s*([A-Za-z_]\w*)\s*:", RegexOptions.Compiled);

        var violations = new List<string>();
        foreach (var path in TvOsSourceFiles())
        {
            var content = StripCommentsAndLiterals(File.ReadAllText(path));
            var fileName = Path.GetFileName(path);
            foreach (Match match in argumentName.Matches(content))
            {
                var name = match.Groups[1].Value;
                if (!parameterNames.Contains(name)
                    && caseInsensitive.TryGetValue(name, out var declared))
                {
                    violations.Add($"{fileName}: {name}: (Parameter: {declared})");
                }
            }
        }

        Assert.Empty(violations);
    }

    // Sammelt die Parameternamen aller positionalen Record-Konstruktoren
    // des tvOS-Projekts und seiner Projektreferenzen (VideoPlayer.Mvu,
    // VideoPlayer.Storage, VideoPlayer.ApiClient, msTools.DeveloperApi).
    private static HashSet<string> RecordConstructorParameterNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var declaration = new Regex(
            @"\brecord\s+(?:struct\s+)?\w+\s*(?:<[^>]*>\s*)?\(",
            RegexOptions.Compiled);

        foreach (var project in new[]
        {
            "VideoPlayer.TvOS", "VideoPlayer.Mvu", "VideoPlayer.Storage",
            "VideoPlayer.ApiClient", "msTools.DeveloperApi"
        })
        {
            var directory = Path.Combine(Root, project);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var path in SourceFiles(directory))
            {
                var content = StripCommentsAndLiterals(File.ReadAllText(path));
                foreach (Match match in declaration.Matches(content))
                {
                    foreach (var name in ParameterNames(content, match.Index + match.Length - 1))
                    {
                        names.Add(name);
                    }
                }
            }
        }

        return names;
    }

    // Zerlegt die ab dem öffnenden »(«-Index liegende Parameterliste eines
    // positionalen Records in die deklarierten Parameternamen (letzter
    // Bezeichner je Top-Level-Komma-Segment, Vorgabewerte abgeschnitten).
    private static IEnumerable<string> ParameterNames(string content, int openParen)
    {
        var depth = 0;
        var closeParen = -1;
        for (var index = openParen; index < content.Length; index++)
        {
            if (content[index] == '(')
            {
                depth++;
            }
            else if (content[index] == ')')
            {
                depth--;
                if (depth == 0)
                {
                    closeParen = index;
                    break;
                }
            }
        }

        if (closeParen < 0)
        {
            yield break;
        }

        var segment = new System.Text.StringBuilder();
        depth = 0;
        var segments = new List<string>();
        for (var index = openParen + 1; index < closeParen; index++)
        {
            var ch = content[index];
            if (ch is '(' or '[' or '{' or '<')
            {
                depth++;
            }
            else if (ch is ')' or ']' or '}' or '>')
            {
                depth--;
            }

            if (ch == ',' && depth == 0)
            {
                segments.Add(segment.ToString());
                segment.Clear();
            }
            else
            {
                segment.Append(ch);
            }
        }

        segments.Add(segment.ToString());
        var nameAtEnd = new Regex(@"([A-Za-z_]\w*)\s*$", RegexOptions.Compiled);
        foreach (var part in segments)
        {
            var withoutDefault = part.Split('=')[0].Trim();
            var match = nameAtEnd.Match(withoutDefault);
            if (match.Success)
            {
                yield return match.Groups[1].Value;
            }
        }
    }

    // Entfernt Kommentare, Zeichenfolgen- und Zeichenliterale, damit die
    // Mustervergleiche nur Code (keine Prosa/Texte) sehen.
    private static string StripCommentsAndLiterals(string content)
    {
        var stripped = Regex.Replace(content, @"@""([^""]|"""")*""", "\"\"");
        stripped = Regex.Replace(stripped, @"""(?:\\.|[^""\\])*""", "\"\"");
        stripped = Regex.Replace(stripped, @"'(?:\\.|[^'\\])'", "''");
        stripped = Regex.Replace(stripped, @"//[^\n]*", string.Empty);
        return Regex.Replace(stripped, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
    }

    private static IEnumerable<string> SourceFiles(string directory) =>
        Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
}
