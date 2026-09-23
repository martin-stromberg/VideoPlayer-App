using VideoPlayer.Maui.Services;
using VideoPlayer.Mvu;

namespace VideoPlayer.Maui
{
    /// <summary>
    /// Shell-Navigation der App (Hauptmenü mit Flyout-Einträgen). Registriert
    /// die Routen für Server-Einrichtung, Anmeldemaske, Medien-Details und
    /// Quellen-Browsing und synchronisiert Shell-Navigationen als
    /// <see cref="NavigatedMessage"/> in das MVU-Model. Hinter dem Eintrag
    /// „Startseite" baut die Shell dynamisch je freigegebener Quelle einen
    /// eigenen Menüeintrag auf (Zustand <see cref="AppModel.SourcesOverview"/>);
    /// die Auswahl dispatcht eine <see cref="SourceSelectedMessage"/>.
    /// Schlug das Laden fehl, steht dort stattdessen ein klickbarer
    /// Neuversuch-Eintrag, der eine <see cref="SourcesReloadRequestedMessage"/>
    /// auslöst.
    /// </summary>
    public partial class AppShell : Shell
    {
        private readonly MvuHost mvuHost;
        private readonly PosterImageService posterImageService;
        private readonly List<ShellItem> sourceMenuItems = [];
        private readonly DataTemplate sourceSectionTemplate;
        private readonly DataTemplate sourceErrorTemplate;
        private readonly DataTemplate sourceInfoTemplate;
        private IReadOnlyList<SourceItem> renderedSources = [];
        private SourceMenuKind renderedKind;

        /// <summary>Art der im Flyout dargestellten Quellengruppe.</summary>
        private enum SourceMenuKind
        {
            /// <summary>Keine Quellengruppe (Initial-Stand: offline/nicht verbunden/vor dem ersten Lauf).</summary>
            None,

            /// <summary>Ladezeile: Quellen werden geladen, noch ohne Einträge.</summary>
            Loading,

            /// <summary>Fehlerzeile mit klickbarem Neuversuch.</summary>
            Error,

            /// <summary>Statuszeile: erfolgreich geladen, aber keine Quelle freigegeben.</summary>
            Empty,

            /// <summary>Quelleneinträge (ggf. zusätzlich zur Fehlerzeile).</summary>
            Sources
        }

        /// <summary>Initialisiert eine neue Instanz der <see cref="AppShell"/>-Klasse.</summary>
        /// <param name="mvuHost">MVU-Host für den Dispatch der Navigations-Nachrichten und die Quellen des Menüs.</param>
        /// <param name="posterImageService">Dienst zur Auflösung der Quellen-Icons (<c>api/sourceicons/{id}</c>).</param>
        public AppShell(MvuHost mvuHost, PosterImageService posterImageService)
        {
            InitializeComponent();
            this.mvuHost = mvuHost;
            this.posterImageService = posterImageService;

            // Server-Einrichtung/-verwaltung: kein FlyoutItem, sondern eine
            // auf den Navigationsstack geschobene Route („Server wechseln").
            Routing.RegisterRoute("serversetup", typeof(Pages.ServerSetupPage));

            // Anmeldemaske: ebenfalls ohne Menüeintrag — wird bei
            // Sitzungsablauf über die Shell-Route erreicht (beim Start als
            // Window-Root statt der Shell).
            Routing.RegisterRoute("login", typeof(Pages.LoginPage));

            // Detailseite eines Mediums:
            // „mediadetails?mediaType=…&id=…&title=…[&show=…&season=…&episode=…]",
            // geschoben auf den Stack.
            Routing.RegisterRoute("mediadetails", typeof(Pages.MediaDetailsPage));

            // Quellen-Browsing (Genre-Filter, Suche, seitenweises Nachladen):
            // „sourceoverview?id=…&name=…", geschoben auf den Stack.
            Routing.RegisterRoute("sourceoverview", typeof(Pages.SourceOverviewPage));

            // Wiedergabeseite: „player", geschoben auf den Stack — der
            // Player-State wurde bereits durch PlayRequestedMessage belegt.
            Routing.RegisterRoute("player", typeof(Pages.PlayerPage));

            sourceSectionTemplate = (DataTemplate)Resources["SourceSectionTemplate"];
            sourceErrorTemplate = (DataTemplate)Resources["SourceErrorTemplate"];
            sourceInfoTemplate = (DataTemplate)Resources["SourceInfoTemplate"];

            Navigated += OnShellNavigated;
            mvuHost.ModelChanged += OnModelChanged;

            // Erstbefüllung: die Shell wird lazy erzeugt — ein bereits
            // geladener Quellenstand (LoadSourcesEffect läuft vor dem
            // NavigateToPageEffect) würde sonst erst beim nächsten
            // ModelChanged erscheinen.
            if (MainThread.IsMainThread)
            {
                UpdateSourceMenuItemsSafe();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(UpdateSourceMenuItemsSafe);
            }
        }

        private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
        {
            // Nutzeraktionen im Flyout laufen nativ über Shell; das MVU-Model wird
            // über eine NavigatedMessage synchronisiert.
            if (AppRoutes.TryGetPage(e.Current?.Location?.OriginalString, out var page))
            {
                _ = DispatchNavigatedAsync(page);
            }
        }

        private async Task DispatchNavigatedAsync(AppPage page)
        {
            try
            {
                await mvuHost.DispatchAsync(new NavigatedMessage(page));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] Navigated-Dispatch fehlgeschlagen: {ex.Message}");
            }
        }

        private void OnModelChanged(object? sender, EventArgs e)
        {
            // Das Model wird erst im Main-Thread-Callback gelesen:
            // ModelChanged feuert nach gate.Release() — ein hier
            // gelesener Snapshot könnte durch einen überlappenden
            // Dispatch veraltet sein, bevor der Callback ausgeführt wird.
            MainThread.BeginInvokeOnMainThread(UpdateSourceMenuItemsSafe);
        }

        private void UpdateSourceMenuItemsSafe()
        {
            try
            {
                UpdateSourceMenuItems(mvuHost.Model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] Quellen-Menü-Aufbau fehlgeschlagen: {ex.Message}");
            }
        }

        private void UpdateSourceMenuItems(AppModel model)
        {
            var overview = model.SourcesOverview;
            var kind = MenuKindFor(overview);
            if (kind == renderedKind && SameSources(renderedSources, overview.Sources))
            {
                return;
            }

            RemoveSourceMenuItems();
            if (kind != SourceMenuKind.None)
            {
                InsertSourceMenuItems(overview, kind);
            }

            // Der gerenderte Stand wird erst nach erfolgreichem Aufbau
            // markiert — schlägt das Einfügen fehl, wiederholt ein
            // späterer ModelChanged den Neuaufbau statt das halb
            // aufgebaute Menü dauerhaft zu pinnen.
            renderedSources = overview.Sources;
            renderedKind = kind;
        }

        /// <summary>Bestimmt, welche Art von Quellengruppe das Menü darstellt.</summary>
        /// <param name="overview">Der aktuelle Quellen-Übersichtszustand.</param>
        /// <returns>Die darzustellende Menüart der Quellengruppe.</returns>
        private static SourceMenuKind MenuKindFor(SourcesOverviewState overview)
        {
            // Fehler schlägt Quellen (Retry-Eintrag zusätzlich zu etwaigem
            // Restbestand); vorhandene Einträge schlagen den Ladezustand —
            // bei einem Reload mit erhaltenen Einträgen darf keine
            // Ladezeile die Liste ersetzen. Der Initial-Stand (offline,
            // nicht verbunden, vor dem ersten Lauf) zeigt nichts.
            if (overview.HasError)
            {
                return SourceMenuKind.Error;
            }

            if (overview.Sources.Count > 0)
            {
                return SourceMenuKind.Sources;
            }

            if (overview.IsLoading)
            {
                return SourceMenuKind.Loading;
            }

            return overview.ShowEmpty ? SourceMenuKind.Empty : SourceMenuKind.None;
        }

        private static bool SameSources(IReadOnlyList<SourceItem> rendered, IReadOnlyList<SourceItem> current)
        {
            return rendered.Count == current.Count
                && rendered.Zip(current).All(pair =>
                    pair.First.Id == pair.Second.Id
                    && pair.First.Name == pair.Second.Name
                    && pair.First.IconPictureId == pair.Second.IconPictureId);
        }

        private void RemoveSourceMenuItems()
        {
            foreach (var item in sourceMenuItems)
            {
                Items.Remove(item);
            }

            sourceMenuItems.Clear();
        }

        private void InsertSourceMenuItems(SourcesOverviewState overview, SourceMenuKind kind)
        {
            // Ab Index 1: die dynamische Quellengruppe steht direkt hinter
            // dem FlyoutItem „Startseite" — eine Sektionsüberschrift
            // „Quellen" (eigenes Template als Abschnittstitel statt
            // deaktiviert wirkendem Eintrag, analog der Webanwendung),
            // dann je nach Stand eine Statuszeile bzw. je Quelle ein
            // eigener Eintrag.
            var index = 1;
            var header = new MenuItem { Text = "Quellen", IsEnabled = false };
            Shell.SetMenuItemTemplate(header, sourceSectionTemplate);
            // Die implizite Konvertierung MenuItem → MenuShellItem erzeugt
            // je Aufruf eine neue Instanz — die getrackte Liste muss
            // dieselbe Instanz wie die eingefügte führen.
            ShellItem headerItem = header;
            Items.Insert(index, headerItem);
            sourceMenuItems.Add(headerItem);
            index++;

            switch (kind)
            {
                case SourceMenuKind.Error:
                    // Klickbarer Neuversuch-Eintrag statt unsichtbarem
                    // Fehlschlag — im Fehler-Stil klar von den normalen
                    // Einträgen abgesetzt.
                    var retry = new MenuItem { Text = "Quellen konnten nicht geladen werden — erneut versuchen" };
                    Shell.SetMenuItemTemplate(retry, sourceErrorTemplate);
                    retry.Clicked += (_, _) => _ = DispatchSourcesReloadAsync();
                    index = InsertMenuEntry(retry, index);
                    break;
                case SourceMenuKind.Loading:
                    index = InsertInfoEntry("Quellen werden geladen …", index);
                    break;
                case SourceMenuKind.Empty:
                    index = InsertInfoEntry("Keine Quellen freigegeben", index);
                    break;
            }

            foreach (var source in overview.Sources)
            {
                var entry = new MenuItem { Text = source.Name };
                entry.Clicked += (_, _) =>
                {
                    FlyoutIsPresented = false;
                    _ = DispatchSourceSelectedAsync(source);
                };
                index = InsertMenuEntry(entry, index);

                if (source.IconPictureId is { } pictureId && source.HasIcon)
                {
                    _ = LoadSourceIconAsync(entry, pictureId);
                }
            }
        }

        private int InsertMenuEntry(MenuItem entry, int index)
        {
            ShellItem entryItem = entry;
            Items.Insert(index, entryItem);
            sourceMenuItems.Add(entryItem);
            return index + 1;
        }

        private int InsertInfoEntry(string text, int index)
        {
            var info = new MenuItem { Text = text, IsEnabled = false };
            Shell.SetMenuItemTemplate(info, sourceInfoTemplate);
            return InsertMenuEntry(info, index);
        }

        private async Task DispatchSourcesReloadAsync()
        {
            try
            {
                await mvuHost.DispatchAsync(new SourcesReloadRequestedMessage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] Quellen-Neuladen-Dispatch fehlgeschlagen: {ex.Message}");
            }
        }

        private async Task DispatchSourceSelectedAsync(SourceItem source)
        {
            try
            {
                await mvuHost.DispatchAsync(new SourceSelectedMessage(source));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppShell] Quellenauswahl-Dispatch fehlgeschlagen: {ex.Message}");
            }
        }

        private async Task LoadSourceIconAsync(MenuItem entry, long pictureId)
        {
            try
            {
                var source = await posterImageService.GetSourceIconSourceAsync(pictureId);
                if (source is not null)
                {
                    MainThread.BeginInvokeOnMainThread(() => entry.IconImageSource = source);
                }
            }
            catch (Exception ex)
            {
                // Fehlende Icons sind kein Fehlerzustand — der Quellenname
                // bleibt als Fallback sichtbar.
                System.Diagnostics.Debug.WriteLine($"[AppShell] Quellen-Icon nicht ladbar: {ex.Message}");
            }
        }
    }
}
