namespace VideoPlayer.Mvu;

/// <summary>Ergebnis eines Update-Schritts: neues Model plus auszuführende Effekte.</summary>
/// <param name="Model">Das neue, unveränderliche App-Model.</param>
/// <param name="Effects">Die auszuführenden Effekte (in Reihenfolge).</param>
/// <returns>Der Übergang des Update-Schritts.</returns>
public sealed record AppTransition(AppModel Model, IReadOnlyList<AppEffect> Effects)
{
    /// <summary>Übergang ohne Zustandsänderung und ohne Effekte.</summary>
    /// <param name="model">Das unverändert beizubehaltende Model.</param>
    /// <returns>Übergang mit demselben Model und leerer Effektliste.</returns>
    public static AppTransition Unchanged(AppModel model) => new(model, []);
}
