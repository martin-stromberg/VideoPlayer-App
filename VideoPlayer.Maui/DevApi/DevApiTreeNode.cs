#if DEBUG || DEV_API
namespace VideoPlayer.Maui.DevApi;

/// <summary>
/// Ein Knoten des logischen Elementbaums für <c>GET /tree</c>
/// (AutomationId, Typname, Sichtbarkeit, Bounds und Text).
/// </summary>
public sealed record DevApiTreeNode
{
    /// <summary>AutomationId des Elements; <c>null</c>, wenn keine vergeben ist.</summary>
    public string? AutomationId { get; init; }

    /// <summary>CLR-Typname des Elements (z. B. <c>Button</c>).</summary>
    public required string Type { get; init; }

    /// <summary>Sichtbarkeits-Flag des Elements.</summary>
    public bool IsVisible { get; init; }

    /// <summary>Layout-Position und -Größe des Elements.</summary>
    public DevApiBounds? Bounds { get; init; }

    /// <summary>Anzeigetext des Elements (Label/Button/InputView/MenuItem).</summary>
    public string? Text { get; init; }

    /// <summary>Platzhaltertext eines Eingabefelds.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Untergeordnete Elemente.</summary>
    public IReadOnlyList<DevApiTreeNode> Children { get; init; } = [];

    /// <summary>
    /// Abgeleitete Einträge eines <c>CollectionView</c> (aus dessen
    /// <c>ItemsSource</c>); <c>null</c> bei anderen Elementtypen.
    /// </summary>
    public IReadOnlyList<DevApiTreeItem>? Items { get; init; }
}

/// <summary>Layout-Bounds eines Elements im Elementbaum.</summary>
public sealed record DevApiBounds
{
    /// <summary>X-Position.</summary>
    public double X { get; init; }

    /// <summary>Y-Position.</summary>
    public double Y { get; init; }

    /// <summary>Breite.</summary>
    public double Width { get; init; }

    /// <summary>Höhe.</summary>
    public double Height { get; init; }
}

/// <summary>
/// Ein aus der <c>ItemsSource</c> einer <c>CollectionView</c> abgeleiteter
/// Eintrag (schließt die UIA-Lücke unter Windows).
/// </summary>
public sealed record DevApiTreeItem
{
    /// <summary>Position des Eintrags in der Quelle.</summary>
    public int Index { get; init; }

    /// <summary>CLR-Typname des Datenobjekts.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Anzeigetext (bekannte Anzeige-Property oder <c>ToString()</c>).</summary>
    public string? Text { get; init; }

    /// <summary>Zusatzdetail (z. B. die Adresse eines Server-Eintrags).</summary>
    public string? Detail { get; init; }
}
#endif
