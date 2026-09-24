#if DEBUG || DEV_API
using VideoPlayer.Mvu;
using VideoPlayer.Mvu.DevApi;

namespace VideoPlayer.Maui.DevApi;

/// <summary>
/// Dienst der Developer-API: startet den lokalen Mini-HTTP-Server
/// (ausschließlich <c>127.0.0.1</c>, systemvergebener Port), verdrahtet
/// die vier Endpunkte <c>GET /model</c>, <c>GET /tree</c>,
/// <c>POST /invoke</c> und <c>POST /set-text</c> mit dem MVU-Modell und
/// dem Visual Tree und legt den Port in <c>devapi.json</c> ab.
/// UI-Zugriffe laufen über <see cref="MainThread.InvokeOnMainThreadAsync(Func{Task})"/>.
/// </summary>
public sealed class DevApiService : IDisposable
{
    private readonly MvuHost mvuHost;
    private DevApiHttpServer? server;

    /// <summary>Erstellt den Dienst.</summary>
    /// <param name="mvuHost">MVU-Host als Quelle des App-Modells.</param>
    public DevApiService(MvuHost mvuHost)
    {
        this.mvuHost = mvuHost ?? throw new ArgumentNullException(nameof(mvuHost));
    }

    /// <summary>Der vom System vergebene Port (gültig nach <see cref="Start"/>).</summary>
    public int Port { get; private set; }

    /// <summary>Pfad der geschriebenen Metadatendatei <c>devapi.json</c>.</summary>
    public string? MetadataPath { get; private set; }

    /// <summary>Startet den HTTP-Server und schreibt die Metadatendatei.</summary>
    public void Start()
    {
        if (server is not null)
        {
            return;
        }

        var router = new DevApiRouter();
        router.Map("GET", "/model", HandleModelAsync);
        router.Map("GET", "/tree", HandleTreeAsync);
        router.Map("POST", "/invoke", HandleInvokeAsync);
        router.Map("POST", "/set-text", HandleSetTextAsync);

        server = new DevApiHttpServer(router);
        server.Start();
        Port = server.Port;
        _ = WriteMetadataAsync();
    }

    /// <summary>Stoppt den HTTP-Server.</summary>
    public void Dispose()
    {
        server?.Dispose();
        server = null;
    }

    /// <summary>Liefert den kuratierten Modell-Snapshot (unveränderlicher Record – kein UI-Thread nötig).</summary>
    private Task<DevApiResponse> HandleModelAsync(DevApiRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(DevApiResponse.Json(DevApiModelSnapshot.FromModel(mvuHost.Model)));

    /// <summary>Liefert den logischen Elementbaum der aktuellen Seite.</summary>
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
    private async Task<DevApiResponse> HandleInvokeAsync(DevApiRequest request, CancellationToken cancellationToken)
    {
        if (!request.TryGetJsonBody<InvokeBody>(out var body, out var bodyError)
            || string.IsNullOrWhiteSpace(body?.AutomationId))
        {
            return DevApiResponse.Error(400, bodyError ?? "Der Body benötigt das Feld 'automationId'.");
        }

        return await MainThread.InvokeOnMainThreadAsync(() => InvokeOnPage(body.AutomationId, body.ItemIndex));
    }

    /// <summary>Setzt den Text des Eingabefelds mit der angegebenen AutomationId.</summary>
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

    private DevApiResponse InvokeOnPage(string automationId, int? itemIndex)
    {
        var element = FindElement(automationId, out var pageMissing);
        if (element is null)
        {
            return pageMissing
                ? DevApiResponse.Error(500, "Es gibt kein aktives Fenster mit einer Seite.")
                : DevApiResponse.Error(404, $"Kein Element mit der AutomationId '{automationId}' gefunden.");
        }

        // Item-Auswahl in einer CollectionView (UIA-Lücke: Einträge fehlen im
        // Automation-Baum): SelectedItem setzen feuert SelectionChanged — dieselbe
        // Ereigniskette wie ein Fingertipp auf die Zeile.
        if (itemIndex is not null)
        {
            if (!DevApiVisualTree.TrySelectItem(element, itemIndex.Value, out var selectError))
            {
                return DevApiResponse.Error(400, selectError!);
            }

            return DevApiResponse.Json(new { invoked = true, automationId, itemIndex, type = element.GetType().Name });
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
    private static Page? CurrentPage() =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault()?.Page;

    /// <summary>
    /// Schreibt <c>devapi.json</c> ins Programmverzeichnis
    /// (<see cref="AppContext.BaseDirectory"/>); bei nicht schreibbarem
    /// Ort (andere Plattformen) Fallback in das App-Datenverzeichnis.
    /// </summary>
    private async Task WriteMetadataAsync()
    {
        try
        {
            var metadata = DevApiMetadata.Create(Port);
            var path = Path.Combine(AppContext.BaseDirectory, "devapi.json");
            try
            {
                await metadata.WriteAsync(path);
            }
            catch (Exception)
            {
                // Nicht schreibbares Programmverzeichnis (z. B. andere
                // Plattformen): Fallback in das App-Datenverzeichnis.
                path = Path.Combine(FileSystem.AppDataDirectory, "devapi.json");
                await metadata.WriteAsync(path);
            }

            MetadataPath = path;
        }
        catch (Exception ex)
        {
            // Fehlgeschlagene Metadaten-Ablage ist kein Funktionsfehler der API.
            System.Diagnostics.Debug.WriteLine($"[DevApi] devapi.json konnte nicht geschrieben werden: {ex.Message}");
        }
    }

    private sealed class InvokeBody
    {
        public string? AutomationId { get; set; }

        /// <summary>Optionaler Index eines CollectionView-Eintrags zur Auswahl.</summary>
        public int? ItemIndex { get; set; }
    }

    private sealed class SetTextBody
    {
        public string? AutomationId { get; set; }

        public string? Text { get; set; }
    }
}
#endif
