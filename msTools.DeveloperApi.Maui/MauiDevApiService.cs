#if DEBUG || DEV_API
using msTools.DeveloperApi;

namespace msTools.DeveloperApi.Maui;

/// <summary>
/// MAUI-Dienst der Developer-API: verdrahtet die vier Endpunkte
/// <c>GET /model</c> (über den injizierten Snapshot-Provider),
/// <c>GET /tree</c>, <c>POST /invoke</c> und <c>POST /set-text</c> mit
/// dem Visual Tree und startet den lokalen Mini-HTTP-Server
/// (ausschließlich <c>127.0.0.1</c>, systemvergebener Port) über die
/// <see cref="DevApiServer"/>-Fassade. Der Port liegt danach in
/// <c>devapi.json</c> im Programmverzeichnis. UI-Zugriffe laufen über
/// <see cref="MainThread.InvokeOnMainThreadAsync(Func{Task})"/>.
/// </summary>
public sealed class MauiDevApiService : IDisposable
{
    private readonly Func<object?> modelSnapshotProvider;
    private DevApiServer? server;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="modelSnapshotProvider">
    /// Liefert den serialisierbaren Modell-Snapshot für <c>GET /model</c>;
    /// wird je Anfrage auf dem Request-Thread aufgerufen (muss
    /// thread-sicher lesbar sein). Keine Zugangsdaten oder Geheimnisse
    /// zurückgeben.
    /// </param>
    public MauiDevApiService(Func<object?> modelSnapshotProvider)
    {
        this.modelSnapshotProvider = modelSnapshotProvider ?? throw new ArgumentNullException(nameof(modelSnapshotProvider));
    }

    /// <summary>Der vom System vergebene Port (gültig nach <see cref="Start"/>).</summary>
    public int Port => server?.Port ?? 0;

    /// <summary>Pfad der geschriebenen Metadatendatei <c>devapi.json</c>.</summary>
    public string? MetadataPath => server?.MetadataPath;

    /// <summary>Startet den HTTP-Server und schreibt die Metadatendatei.</summary>
    public void Start()
    {
        if (server is not null)
        {
            return;
        }

        var router = new DevApiRouter();
        router.MapModel(modelSnapshotProvider);
        router.Map("GET", "/tree", HandleTreeAsync);
        router.Map("POST", "/invoke", HandleInvokeAsync);
        router.Map("POST", "/set-text", HandleSetTextAsync);

        server = new DevApiServer(router, AppContext.BaseDirectory, FileSystem.AppDataDirectory);
        server.Start();
    }

    /// <summary>Stoppt den HTTP-Server.</summary>
    public void Dispose()
    {
        server?.Dispose();
        server = null;
    }

    /// <summary>Liefert den logischen Elementbaum der aktuellen Seite.</summary>
    /// <param name="request">Die eingehende Anfrage.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Antwort mit dem Baum oder einer Fehlermeldung.</returns>
    private async Task<DevApiResponse> HandleTreeAsync(DevApiRequest request, CancellationToken cancellationToken)
    {
        var node = await MainThread.InvokeOnMainThreadAsync(() =>
        {
            var page = CurrentPage();
            return page is null ? null : DevApiVisualTree.BuildTree(page);
        });
        return node is null
            ? DevApiResponse.Error(500, "Es gibt kein aktives Fenster mit einer Seite.")
            : DevApiResponse.Json(node);
    }

    /// <summary>Löst das Clicked-Äquivalent des Elements mit der angegebenen AutomationId aus.</summary>
    /// <param name="request">Die eingehende Anfrage mit dem Invoke-Body.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Antwort mit dem Auslöseergebnis oder einer Fehlermeldung.</returns>
    private async Task<DevApiResponse> HandleInvokeAsync(DevApiRequest request, CancellationToken cancellationToken)
    {
        if (!request.TryGetJsonBody<InvokeBody>(out var body, out var bodyError)
            || string.IsNullOrWhiteSpace(body?.AutomationId))
        {
            return DevApiResponse.Error(400, bodyError ?? "Der Body benötigt das Feld 'automationId'.");
        }

        return await MainThread.InvokeOnMainThreadAsync(() => InvokeOnPage(body.AutomationId, body.ItemIndex, body.Gesture));
    }

    /// <summary>Setzt den Text des Eingabefelds mit der angegebenen AutomationId.</summary>
    /// <param name="request">Die eingehende Anfrage mit dem Set-Text-Body.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Antwort mit dem Ergebnis oder einer Fehlermeldung.</returns>
    private async Task<DevApiResponse> HandleSetTextAsync(DevApiRequest request, CancellationToken cancellationToken)
    {
        if (!request.TryGetJsonBody<SetTextBody>(out var body, out var bodyError)
            || string.IsNullOrWhiteSpace(body?.AutomationId)
            || body.Text is null)
        {
            return DevApiResponse.Error(400, bodyError ?? "Der Body benötigt die Felder 'automationId' und 'text'.");
        }

        return await MainThread.InvokeOnMainThreadAsync(() => SetTextOnPage(body.AutomationId, body.Text));
    }

    private DevApiResponse InvokeOnPage(string automationId, int? itemIndex, string? gesture)
    {
        var element = FindElement(automationId, out var pageMissing);
        if (element is null)
        {
            return pageMissing
                ? DevApiResponse.Error(500, "Es gibt kein aktives Fenster mit einer Seite.")
                : DevApiResponse.Error(404, $"Kein Element mit der AutomationId '{automationId}' gefunden.");
        }

        // Item-Auswahl in einer CollectionView oder einem BindableLayout-
        // Container (UIA-Lücke: Einträge fehlen im Automation-Baum):
        // SelectedItem setzen feuert SelectionChanged bzw. der Tap-
        // Recognizer des Eintrags wird ausgelöst — dieselbe
        // Ereigniskette wie ein Fingertipp auf die Zeile. Die optionale
        // Geste "longpress" löst stattdessen die Long-Press-Auswahl aus.
        if (itemIndex is not null)
        {
            if (!DevApiVisualTree.TrySelectItem(element, itemIndex.Value, gesture, out var selectError))
            {
                return DevApiResponse.Error(400, selectError!);
            }

            return DevApiResponse.Json(new { invoked = true, automationId, itemIndex, gesture, type = element.GetType().Name });
        }

        if (!DevApiVisualTree.TryInvoke(element, out var error))
        {
            return DevApiResponse.Error(400, error!);
        }

        return DevApiResponse.Json(new { invoked = true, automationId, type = element.GetType().Name });
    }

    private DevApiResponse SetTextOnPage(string automationId, string text)
    {
        var element = FindElement(automationId, out var pageMissing);
        if (element is null)
        {
            return pageMissing
                ? DevApiResponse.Error(500, "Es gibt kein aktives Fenster mit einer Seite.")
                : DevApiResponse.Error(404, $"Kein Element mit der AutomationId '{automationId}' gefunden.");
        }

        if (!DevApiVisualTree.TrySetText(element, text, out var error))
        {
            return DevApiResponse.Error(400, error!);
        }

        return DevApiResponse.Json(new { set = true, automationId, type = element.GetType().Name });
    }

    private Element? FindElement(string automationId, out bool pageMissing)
    {
        var page = CurrentPage();
        pageMissing = page is null;
        return page is null ? null : DevApiVisualTree.FindByAutomationId(page, automationId);
    }

    /// <summary>Die aktuell angezeigte Seite des ersten Fensters (LoadingPage oder AppShell).</summary>
    /// <returns>Die aktuelle Seite oder <c>null</c>, wenn kein Fenster existiert.</returns>
    private static Page? CurrentPage() =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault()?.Page;

    private sealed class InvokeBody
    {
        public string? AutomationId { get; set; }

        /// <summary>Optionaler Index eines Listeneintrags (CollectionView oder BindableLayout-Container) zur Auswahl.</summary>
        public int? ItemIndex { get; set; }

        /// <summary>Optionale Geste der Item-Auswahl (<c>"tap"</c> = Standard, <c>"longpress"</c> = 2-s-Halte-Äquivalent).</summary>
        public string? Gesture { get; set; }
    }

    private sealed class SetTextBody
    {
        public string? AutomationId { get; set; }

        public string? Text { get; set; }
    }
}
#endif
