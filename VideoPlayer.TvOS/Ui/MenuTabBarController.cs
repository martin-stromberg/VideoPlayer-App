using UIKit;
using VideoPlayer.Mvu;

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

        /// <summary>Initialisiert eine neue Instanz der <see cref="MenuTabBarController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        public MenuTabBarController(Func<AppMessage, Task> dispatch)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var controllers = AppRoutes.MenuPages
                .Select(page => new PagePlaceholderViewController(
                    PageTitles[page].Title,
                    PageTitles[page].Subtitle)
                {
                    TabBarItem = new UITabBarItem(PageTitles[page].Title, null, (nint)AppRoutes.MenuPages.IndexOf(page))
                })
                .Cast<UIViewController>()
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
            var index = AppRoutes.MenuPages.IndexOf(page);
            if (index < 0)
            {
                return false;
            }

            SelectedIndex = index;
            return true;
        }
    }
}
