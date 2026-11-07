using System.ComponentModel;

namespace VideoPlayer.Mvu;

/// <summary>
/// Treibt den MVU-Zyklus: nimmt <see cref="AppMessage"/>s entgegen, wendet
/// <see cref="AppUpdate.Update"/> an, stellt das neue <see cref="AppModel"/>
/// für Bindings bereit und delegiert Effekte an den <see cref="IAppEffectHandler"/>.
/// Dispatchs werden serialisiert abgearbeitet.
/// </summary>
public sealed class MvuHost : INotifyPropertyChanged
{
    private readonly IAppEffectHandler effectHandler;
    private readonly Func<AppModel, AppMessage, AppTransition> update;
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Erstellt den Host.</summary>
    /// <param name="effectHandler">Handler, der die Effekte am App-Rand ausführt.</param>
    /// <param name="update">Update-Funktion des MVU-Zyklus; <c>null</c> = <see cref="AppUpdate.Update"/> (für Tests ersetzbar).</param>
    public MvuHost(
        IAppEffectHandler effectHandler,
        Func<AppModel, AppMessage, AppTransition>? update = null)
    {
        this.effectHandler = effectHandler ?? throw new ArgumentNullException(nameof(effectHandler));
        this.update = update ?? AppUpdate.Update;
    }

    /// <summary>Aktuelles, unveränderliches App-Model.</summary>
    /// <value>Das zuletzt angewendete Model.</value>
    public AppModel Model { get; private set; } = new();

    /// <summary>Wird nach jedem angewendeten Update ausgelöst.</summary>
    public event EventHandler? ModelChanged;

    /// <summary>Benachrichtigt Bindings über die Änderung von <see cref="Model"/>.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Wendet eine Nachricht auf das Model an und führt die resultierenden
    /// Effekte nacheinander aus.
    /// </summary>
    /// <param name="message">Die anzuwendende Nachricht.</param>
    /// <param name="cancellationToken">Bricht den Dispatch bzw. die Effektausführung ab.</param>
    public async Task DispatchAsync(AppMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        IReadOnlyList<AppEffect> effects;
        await gate.WaitAsync(cancellationToken);
        try
        {
            var transition = update(Model, message);
            Model = transition.Model;
            effects = transition.Effects;
        }
        finally
        {
            gate.Release();
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Model)));
        ModelChanged?.Invoke(this, EventArgs.Empty);

        foreach (var effect in effects)
        {
            await effectHandler.HandleAsync(effect, cancellationToken);
        }
    }
}
