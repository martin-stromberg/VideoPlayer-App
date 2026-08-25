using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using CommunityToolkit.Maui;
using VideoWebPlayer.Client;
using VideoWebPlayer.Maui.Services;
using VideoWebPlayer.Maui.Services.Events;

namespace VideoWebPlayer.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement(true);

            // Registriere HttpClient mit MauiVideoWebPlayerClient
            // Die BaseAddress wird zur Laufzeit aus Preferences gesetzt
            builder.Services.AddHttpClient<MauiVideoWebPlayerClient>((sp, client) =>
            {
                // Versuche die BaseAddress aus SettingsService (profil-/umgebungsabhängig) zu laden
				var settings = sp.GetRequiredService<Services.ISettingsService>();
				var serverAddress = settings.ServerAddress?.ToLower() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(serverAddress))
                {
                    // Stelle sicher, dass die URL mit http:// oder https:// beginnt
                    if (!serverAddress.StartsWith("http://") && !serverAddress.StartsWith("https://"))
                    {
                        serverAddress = $"http://{serverAddress}";
                    }
                    
                    client.BaseAddress = new Uri(serverAddress);
                }
                
                MauiClientConfiguration.ApplyClientGateHeader(client);
            });

            // Registriere MauiVideoWebPlayerClient auch als VideoWebPlayerClient für Dependency Injection
            builder.Services.AddScoped<VideoWebPlayerClient>(sp => sp.GetRequiredService<MauiVideoWebPlayerClient>());

            // register services and pages
            builder.Services.AddSingleton<Services.ISettingsService, Services.SettingsService>();
            builder.Services.AddSingleton<Services.IConnectionService, Services.ConnectionService>();
            builder.Services.AddSingleton<Services.IAuthService, Services.AuthService>();
            builder.Services.AddSingleton<Services.SignalRService>();
			builder.Services.AddSingleton<Services.WatchlistDownloadCoordinatorService>();
            
            // Register Notification Event Service
            builder.Services.AddSingleton<NotificationEventService>(sp =>
            {
                var signalRService = sp.GetService<SignalRService>();
                return new NotificationEventService(signalRService);
            });
            builder.Services.AddSingleton<IPublishNotificationEvent>(sp => sp.GetRequiredService<NotificationEventService>());
            builder.Services.AddSingleton<ISubscribeNotificationEvent>(sp => sp.GetRequiredService<NotificationEventService>());

            builder.Services.AddTransient<ServerSetupPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoadingPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<HomePage>();

            builder.ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var mauiApp = builder.Build();
            // expose the built service provider to the running App
            App.ServiceProvider = mauiApp.Services;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                (Application.Current as App)?.InitializeAfterServices(App.ServiceProvider!);
            });

            return mauiApp;
        }
    }
}
