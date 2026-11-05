using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Seitenrahmen der Lean-back-App: Menüleiste (Tab-Leiste, tvOS-idiomatisch
    /// oben) plus Inhaltsbereich — vollständig über die tvOS-Focus-Engine per
    /// Siri Remote steuerbar. Die Tabs folgen <see cref="AppRoutes.MenuPages"/>
    /// in derselben Reihenfolge wie das Shell-Hauptmenü der MAUI-App; jede
    /// Auswahl meldet <see cref="NavigatedMessage"/> in den MVU-Zyklus.
    /// </summary>
    public sealed class MenuTabBarController : UITabBarController
    {
        private static readonly IReadOnlyDictionary<AppPage, (string Title, string Subtitle)> PageTitles =
            new Dictionary<AppPage, (string, string)>
            {
                [AppPage.Home] = ("Startseite", "Weiterschauen, Favoriten und neue Inhalte."),
                [AppPage.Favorites] = ("Favoriten", "Ihre markierten Inhalte."),
                [AppPage.Playlists] = ("Playlists", "Eigene und öffentliche Playlists."),
                [AppPage.Downloads] = ("Downloads", "Lokal verfügbare Inhalte."),
                [AppPage.Profile] = ("Profil", "Konto- und Profilinformationen."),
                [AppPage.Settings] = ("Einstellungen", "App- und Wiedergabe-Einstellungen.")
            };

        private readonly Func<AppMessage, Task> dispatch;
        private readonly TvosPosterImageService posters;

        /// <summary>Initialisiert eine neue Instanz der <see cref="MenuTabBarController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Poster-Cache-/Ladedienst der Startseiten-Karussells.</param>
        public MenuTabBarController(Func<AppMessage, Task> dispatch, TvosPosterImageService posters)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.posters = posters ?? throw new ArgumentNullException(nameof(posters));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var controllers = AppRoutes.MenuPages
                .Select(CreatePageController)
                .ToArray();
            ViewControllers = controllers;

            ViewControllerSelected += (_, e) =>
            {
                var index = Array.IndexOf(controllers, e.ViewController);
                if (index >= 0)
                {
                    _ = dispatch(new NavigatedMessage(AppRoutes.MenuPages[index]));
                }
            };
        }

        /// <summary>Wählt den Tab der angegebenen Menüseite aus (programmatische Navigation).</summary>
        /// <param name="page">Zielseite.</param>
        /// <returns><c>true</c>, wenn die Seite einem Menüeintrag entspricht.</returns>
        public bool Select(AppPage page)
        {
            var index = IndexOfMenuPage(page);
            if (index < 0)
            {
                return false;
            }

            SelectedIndex = index;
            return true;
        }

        /// <summary>Verteilt den Modellstand an die modellgetriebenen Menüseiten.</summary>
        /// <param name="model">Das aktuelle App-Modell.</param>
        public void Render(AppModel model)
        {
            if (ViewControllers is null)
            {
                return;
            }

            foreach (var controller in ViewControllers)
            {
                if (controller is IModelRenderable renderable)
                {
                    renderable.Render(model);
                }
            }
        }

        /// <summary>Erzeugt den Inhaltscontroller einer Menüseite samt Tab-Eintrag.</summary>
        /// <param name="page">Die Menüseite.</param>
        /// <returns>Der Seitencontroller für die Tab-Leiste.</returns>
        private UIViewController CreatePageController(AppPage page)
        {
            UIViewController controller = page switch
            {
                AppPage.Home => new HomePageViewController(dispatch, posters),
                AppPage.Profile => new ProfilePageViewController(dispatch),
                AppPage.Settings => new SettingsPageViewController(dispatch),
                _ => new PagePlaceholderViewController(
                    PageTitles[page].Title,
                    PageTitles[page].Subtitle)
            };
            controller.TabBarItem = new UITabBarItem(
                PageTitles[page].Title,
                null,
                IndexOfMenuPage(page));
            return controller;
        }

        /// <summary>Sucht die Position einer Menüseite in <see cref="AppRoutes.MenuPages"/>.</summary>
        /// <param name="page">Die gesuchte Menüseite.</param>
        /// <returns>Der Zeilenindex oder <c>-1</c>, wenn die Seite kein Menüeintrag ist.</returns>
        private static int IndexOfMenuPage(AppPage page)
        {
            // AppRoutes.MenuPages ist IReadOnlyList<AppPage> und besitzt
            // kein IndexOf-Mitglied.
            for (var index = 0; index < AppRoutes.MenuPages.Count; index++)
            {
                if (AppRoutes.MenuPages[index] == page)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
