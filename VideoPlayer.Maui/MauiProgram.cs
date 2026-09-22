using Microsoft.Extensions.Logging;
using VideoPlayer.Maui.Services;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();

            // MVU-Zyklus: Host hält das unveränderliche App-Model, der Effekt-Handler
            // führt Navigation und Server-Erreichbarkeitsprüfung am App-Rand aus.
            builder.Services.AddSingleton<IServerAddressProvider, ServerAddressProvider>();
            builder.Services.AddSingleton<IServerHealthProbe, ServerHealthProbe>();
            builder.Services.AddSingleton<IAppEffectHandler, ShellAppEffectHandler>();
            builder.Services.AddSingleton<MvuHost>();

            // Lokale Datenhaltung: Profil-Isolation, normale und sichere Ablage,
            // Einstellungs-/Geheimnisdienste und getrennte Datenbanken.
            builder.Services.AddSingleton(_ => AppProfile.FromEnvironment());
            builder.Services.AddSingleton<ISettingsStore>(_ => new PreferencesSettingsStore(Preferences.Default));
            builder.Services.AddSingleton<ISecretStore>(_ => new SecureStorageSecretStore(SecureStorage.Default));
            builder.Services.AddSingleton<AppSettingsService>();
            builder.Services.AddSingleton<AppSecretsService>();
            builder.Services.AddSingleton(sp =>
                AppStorageLayoutProvider.CreateLayout(sp.GetRequiredService<AppProfile>()));
            builder.Services.AddSingleton<BackupDatabase>();
            builder.Services.AddSingleton<CacheDatabase>();

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<Pages.LoadingPage>();
            builder.Services.AddTransient<Pages.HomePage>();
            builder.Services.AddTransient<Pages.SourcesPage>();
            builder.Services.AddTransient<Pages.FavoritesPage>();
            builder.Services.AddTransient<Pages.DownloadsPage>();
            builder.Services.AddTransient<Pages.ProfilePage>();
            builder.Services.AddTransient<Pages.SettingsPage>();

            builder.ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
