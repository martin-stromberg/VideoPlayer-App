using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Modellgetriebene Seite innerhalb des Menürahmens: der
    /// <see cref="MenuTabBarController"/> verteilt den aktuellen
    /// <see cref="AppModel"/>-Stand an alle Kindcontroller, die dieses
    /// Interface implementieren.
    /// </summary>
    public interface IModelRenderable
    {
        /// <summary>Spiegelt den aktuellen Modellstand auf die Seite.</summary>
        /// <param name="model">Das aktuelle App-Modell.</param>
        void Render(AppModel model);
    }
}
