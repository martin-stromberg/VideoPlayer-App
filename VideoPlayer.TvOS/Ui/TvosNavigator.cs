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

        private MainViewController? main;
        private MenuTabBarController? tabBar;
        private ServerSetupViewController? serverSetupPage;
        private LoginViewController? loginPage;

        /// <summary>Die zuletzt dargestellte Menüseite — Rückziel der Menü-Taste auf Workflow-Seiten.</summary>
        private AppPage LastMenuPage { get; set; } = AppPage.Home;

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosNavigator"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        public TvosNavigator(Func<AppMessage, Task> dispatch)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
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
                    tabBar ??= new MenuTabBarController(dispatch);
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
