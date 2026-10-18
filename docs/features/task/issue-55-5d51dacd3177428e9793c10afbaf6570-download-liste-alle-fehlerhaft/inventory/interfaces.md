# Interfaces

Für die Anforderung sind keine Interface-Änderungen erforderlich. `DownloadService` ist eine konkrete Singleton-Klasse (Registrierung via `AddSingleton<DownloadService>` in `MauiProgram`); die Effekt-Verdrahtung ruft die öffentlichen Methoden direkt auf. `IDownloadBackend`/`INetworkStatusMonitor` sind von der Löschaktion nicht betroffen.
