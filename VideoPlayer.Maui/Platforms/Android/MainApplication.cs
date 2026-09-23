using Android.App;
using Android.Runtime;

namespace VideoPlayer.Maui
{
    /// <summary>Android-Anwendungsklasse; erzeugt die MAUI-App über <see cref="MauiProgram"/>.</summary>
    [Application]
    public class MainApplication : MauiApplication
    {
        /// <summary>Initialisiert die Anwendung mit dem JNI-Handle.</summary>
        /// <param name="handle">JNI-Handle der zugrunde liegenden Java-Instanz.</param>
        /// <param name="ownership">Besitzverhältnis des Handles.</param>
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        /// <inheritdoc/>
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
