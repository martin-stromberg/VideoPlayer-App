using Android.App;
using Android.Runtime;

namespace VideoWebPlayer.Maui
{
    /// <summary>
    /// Application-Klasse der Android-App; erstellt die MAUI-Anwendung.
    /// </summary>
    [Application]
    public class MainApplication : MauiApplication
    {
        /// <summary>
        /// Erstellt eine neue Instanz von <see cref="MainApplication"/>.
        /// </summary>
        /// <param name="handle">JNI-Handle der zugrunde liegenden Java-Instanz.</param>
        /// <param name="ownership">Besitzverhältnis des JNI-Handles.</param>
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        /// <inheritdoc/>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
