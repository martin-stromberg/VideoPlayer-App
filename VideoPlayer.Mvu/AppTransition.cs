namespace VideoPlayer.Mvu;

/// <summary>Ergebnis eines Update-Schritts: neues Model plus auszuführende Effekte.</summary>
public sealed record AppTransition(AppModel Model, IReadOnlyList<AppEffect> Effects)
{
    /// <summary>Übergang ohne Zustandsänderung und ohne Effekte.</summary>
    public static AppTransition Unchanged(AppModel model) => new(model, []);
}
