using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Modellgetriebene Navigation des tvOS-Seitenrahmens — Pendant zur
    /// <c>NavigateAsync</c>-Logik des <c>ShellAppEffectHandler</c>: bildet
    /// <see cref="AppModel.CurrentPage"/> und den Verbindungszustand auf den
    /// Fensterinhalt ab (Ladeanzeige, Verbindungs-Workflow-Seiten oder den
    /// Tab-Rahmen). Alle Darstellungswechsel sind idempotent.
    /// </summary>
    public sealed class TvosNavigator
    {
        private readonly Func<AppMessage, Task> dispatch;
        private readonly TvosPosterImageService posters;

        private MainViewController? main;
        private MenuTabBarController? tabBar;
        private ServerSetupViewController? serverSetupPage;
        private LoginViewController? loginPage;
        private SourceBrowserViewController? sourceBrowserPage;

        /// <summary>Die zuletzt dargestellte Menüseite — Rückziel der Menü-Taste auf Workflow-Seiten.</summary>
        private AppPage LastMenuPage { get; set; } = AppPage.Home;

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosNavigator"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Poster-Cache-/Ladedienst der Startseiten-Karussells.</param>
        public TvosNavigator(Func<AppMessage, Task> dispatch, TvosPosterImageService posters)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.posters = posters ?? throw new ArgumentNullException(nameof(posters));
        }

        /// <summary>Verbindet den Navigator mit dem Fensterrahmen.</summary>
        /// <param name="main">Der containerisierende Haupt-ViewController.</param>
        public void Attach(MainViewController main)
        {
            this.main = main ?? throw new ArgumentNullException(nameof(main));
        }

        /// <summary>
        /// Stellt die angegebene Seite dar: <see cref="AppPage.Loading"/>,
        /// <see cref="AppPage.ServerSetup"/> und <see cref="AppPage.Login"/>
        /// als Einzelansicht (ohne Menü), Menüseiten als Tab-Auswahl im
        /// <see cref="MenuTabBarController"/>.
        /// </summary>
        /// <param name="page">Die darzustellende Seite.</param>
        /// <returns>Eine abgeschlossene Aufgabe.</returns>
        public Task NavigateTo(AppPage page)
        {
            if (main is null)
            {
                return Task.CompletedTask;
            }

            switch (page)
            {
                case AppPage.Loading:
                    main.ShowLoading();
                    return Task.CompletedTask;
                case AppPage.ServerSetup:
                    serverSetupPage ??= new ServerSetupViewController(dispatch, NavigateBackToMenu);
                    main.Show(serverSetupPage);
                    return Task.CompletedTask;
                case AppPage.Login:
                    loginPage ??= new LoginViewController(dispatch, NavigateBackToMenu);
                    main.Show(loginPage);
                    return Task.CompletedTask;
                default:
                    tabBar ??= new MenuTabBarController(dispatch, posters);
                    if (tabBar.Select(page))
                    {
                        LastMenuPage = page;
                        main.Show(tabBar);
                    }

                    return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Spiegelt den aktuellen Modellstand auf die sichtbare Seite:
        /// stellt <see cref="AppModel.CurrentPage"/> dar (idempotent) und
        /// verteilt das Modell an die Workflow-Seiten und die
        /// modellgetriebenen Menüseiten.
        /// </summary>
        /// <param name="model">Das aktuelle App-Modell.</param>
        public void Render(AppModel model)
        {
            _ = NavigateTo(model.CurrentPage);
            main?.UpdateLoadingStatus(model.StartupStatusText, model.StorageNotice);
            serverSetupPage?.Render(model);
            loginPage?.Render(model);
            tabBar?.Render(model);
            sourceBrowserPage?.Render(model);
        }

        /// <summary>
        /// Präsentiert die Browsing-Seite einer Quelle modal über dem
        /// Menürahmen (<see cref="NavigateToSourceOverviewEffect"/>) —
        /// Gegenstück zur Shell-Route <c>sourceoverview</c> der
        /// MAUI-App. Die Seite wird einmal erzeugt und bei erneuter
        /// Präsentation mit dem neuen Quellen-Kontext versehen statt
        /// gestapelt (Duplikat-Guard: eine bereits präsentierte
        /// Browser-Seite bleibt unverändert).
        /// </summary>
        /// <param name="sourceId">Id der zu öffnenden Quelle.</param>
        /// <param name="sourceName">Anzeigename der Quelle (Seitentitel).</param>
        public void PresentSourceBrowser(long sourceId, string sourceName)
        {
            if (main is null)
            {
                return;
            }

            sourceBrowserPage ??= new SourceBrowserViewController(dispatch, posters)
            {
                ModalPresentationStyle = UIModalPresentationStyle.FullScreen
            };
            sourceBrowserPage.SetSource(sourceId, sourceName);

            var host = TopmostPresented();
            if (host is SourceBrowserViewController
                || sourceBrowserPage.PresentingViewController is not null)
            {
                // Die Browser-Seite ist bereits präsentiert (oben oder
                // unter einem Folgeziel) — nur der Quellen-Kontext
                // wurde aktualisiert; ein erneutes Presentieren eines
                // bereits präsentierten Controllers wäre ein
                // UIKit-Verstoß.
                return;
            }

            host.PresentViewController(sourceBrowserPage, true, null);
        }

        /// <summary>
        /// Präsentiert das vorbereitete Navigationsziel einer
        /// Eintrags-Auswahl: Detailansicht und Wiedergabe entstehen in
        /// späteren Entwicklungsschritten — bis dahin erscheint ein
        /// modaler Platzhalter mit Titel und Hinweis (kein Fehler, kein
        /// No-Op). Die Präsentation ist modal, damit der modellgetriebene
        /// <see cref="MainViewController.Show"/>-Rahmen das Ziel beim
        /// nächsten <see cref="Render"/> nicht sofort wieder verdrängt;
        /// ein bereits präsentiertes Ziel wird mit dem Folgeziel
        /// aktualisiert statt gestapelt (Effekt-Folge Detail →
        /// Wiedergabe der Weiterschauen-Einträge).
        /// </summary>
        /// <param name="title">Titel des angesteuerten Eintrags.</param>
        /// <param name="detail">Hinweistext des vorbereiteten Zielzustands.</param>
        public void PresentPreparedTarget(string title, string detail)
        {
            if (main is null)
            {
                return;
            }

            var host = TopmostPresented();
            if (host is PreparedTargetViewController existing)
            {
                existing.Update(title, detail);
                return;
            }

            var target = new PreparedTargetViewController(title, detail)
            {
                ModalPresentationStyle = UIModalPresentationStyle.FullScreen
            };
            // Auf dem obersten präsentierten Controller stapeln — die
            // Eintrags-Auswahl der modalen Browser-Seite muss das Ziel
            // über dem Browser zeigen können (Rückkehr führt dorthin).
            host.PresentViewController(target, true, null);
        }

        /// <summary>
        /// Ermittelt den obersten Controller der modalen
        /// Präsentationskette (<see cref="MainViewController"/> selbst,
        /// wenn nichts präsentiert ist) — Präsentationsanker für
        /// modale Ziele über dem Menürahmen oder der Browser-Seite.
        /// </summary>
        /// <returns>Der oberste präsentierte Controller bzw. der Fensterrahmen.</returns>
        private UIViewController TopmostPresented()
        {
            UIViewController host = main!;
            while (host.PresentedViewController is { } presented)
            {
                host = presented;
            }

            return host;
        }

        /// <summary>
        /// Rückkehr von einer Workflow-Seite zur zuletzt dargestellten
        /// Menüseite — die Zustandsprüfung des Updates
        /// (<c>UpdateNavigateTo</c>) entscheidet, ob die Navigation
        /// tatsächlich stattfindet.
        /// </summary>
        /// <returns>Die Dispatch-Aufgabe der Navigationsanforderung.</returns>
        private Task NavigateBackToMenu() =>
            dispatch(new NavigateToMessage(LastMenuPage));
    }
}
