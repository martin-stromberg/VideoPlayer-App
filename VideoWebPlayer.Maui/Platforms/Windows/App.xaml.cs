using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VideoWebPlayer.Maui.WinUI
{
    /// <summary>
    /// Stellt anwendungsspezifisches Verhalten bereit, das die Standard-Application-Klasse ergänzt.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initialisiert das Singleton-Anwendungsobjekt. Dies ist die erste Zeile des eigenen Codes,
        /// der ausgeführt wird, und damit das logische Äquivalent zu main() bzw. WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <inheritdoc/>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
