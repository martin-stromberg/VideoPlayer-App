using CoreGraphics;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Menüseite „Startseite" der Lean-back-App: vertikal gestapelte,
    /// horizontal per Siri Remote fokussierbare Karussell-Reihen
    /// „Weiterschauen" (mit Fortschrittsbalken), „Favoriten" und
    /// „Neu im Programm" sowie die saisonalen Genre-Sektionen —
    /// modellgetrieben aus dem MVU-Cache-Stand (Cache-First:
    /// gespeicherte Listen erscheinen sofort, Aktualisierung über
    /// Synchronisation und Server-Ereignisse im Hintergrund). Im
    /// Offline-Zustand zeigt die Seite die aus lokalen Daten gespeisten
    /// Bereiche samt Hinweis-Banner; leere Listen blenden ihre Sektion
    /// aus. Die Auswahl dispatcht
    /// <see cref="ContinueWatchingItemSelectedMessage"/> bzw.
    /// <see cref="MediaItemSelectedMessage"/> — Detailansicht und
    /// Wiedergabe landen auf dem vorbereiteten Navigationsziel.
    /// </summary>
    public sealed class HomePageViewController : UIViewController, IModelRenderable
    {
        // Kartengröße der Weiterschauen-Reihe (querformatige 16:9-Stills plus Titelbereich).
        private static readonly CGSize ContinueWatchingItemSize = new(480f, 334f);

        // Kartengröße der Poster-Reihen (2:3-Poster plus Titelbereich).
        private static readonly CGSize PosterItemSize = new(300f, 514f);

        private readonly Func<AppMessage, Task> dispatch;
        private readonly TvosPosterImageService posters;
        private readonly TimeProvider timeProvider;

        private UIStackView? content;
        private UILabel? offlineBanner;
        private UILabel? noticeBanner;
        // Die Quellen liegen seit dem Wizard-Umbau auf der eigenen
        // Menüseite „Quellen" (SourcesPageViewController).
        private readonly List<HomeCarouselView> sections = [];
        private string[] appliedKeys = [];
        private AppModel? lastModel;
        private ITimer? expiryTimer;

        /// <summary>Initialisiert eine neue Instanz der <see cref="HomePageViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Poster-Cache-/Ladedienst der Karten.</param>
        public HomePageViewController(Func<AppMessage, Task> dispatch, TvosPosterImageService posters)
            : this(dispatch, posters, TimeProvider.System)
        {
        }

        /// <summary>Initialisiert eine neue Instanz der <see cref="HomePageViewController"/>-Klasse mit injizierbarer Uhr (Ablaufgrenzen saisonaler Sektionen).</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Poster-Cache-/Ladedienst der Karten.</param>
        /// <param name="timeProvider">Uhr für serverseitige Ablaufgrenzen.</param>
        public HomePageViewController(Func<AppMessage, Task> dispatch, TvosPosterImageService posters, TimeProvider timeProvider)
        {
            Title = "Startseite";
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.posters = posters ?? throw new ArgumentNullException(nameof(posters));
            this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            var scrollView = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                ShowsVerticalScrollIndicator = false,
                ClipsToBounds = false
            };
            View.AddSubview(scrollView);

            content = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Fill,
                Distribution = UIStackViewDistribution.Fill,
                Spacing = 30,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            scrollView.AddSubview(content);

            offlineBanner = TvosControls.Body("Offline — lokale Daten; keine Serververbindung.");
            offlineBanner.TextColor = UIColor.SystemYellow;
            offlineBanner.Hidden = true;

            noticeBanner = TvosControls.Body();
            noticeBanner.TextColor = UIColor.SystemYellow;
            noticeBanner.Hidden = true;

            content.AddArrangedSubview(TvosControls.Title("Startseite"));
            content.AddArrangedSubview(offlineBanner);
            content.AddArrangedSubview(noticeBanner);

            NSLayoutConstraint.ActivateConstraints(
            [
                scrollView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                scrollView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -40),
                scrollView.TopAnchor.ConstraintEqualTo(View.TopAnchor, 150),
                scrollView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor, -40),

                content.LeadingAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.LeadingAnchor),
                content.TrailingAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.TrailingAnchor),
                content.TopAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.TopAnchor),
                content.BottomAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.BottomAnchor),
                content.WidthAnchor.ConstraintEqualTo(scrollView.FrameLayoutGuide.WidthAnchor)
            ]);

            // Vorsorglich geladenes Modell übernehmen (Render läuft bei
            // unsichtbarem Tab nicht ins Leere — IsViewLoaded-Guard).
            if (lastModel is not null)
            {
                ApplyRender(lastModel);
            }
        }

        /// <inheritdoc/>
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            // Cache-Stand beim Sichtbarwerden auffrischen (Muster:
            // HomePage.OnAppearing der MAUI-App) — Cache-First, also
            // ohne sichtbaren Ladezustand.
            _ = dispatch(new HomeListsReloadRequestedMessage());
            if (lastModel is not null)
            {
                ScheduleExpiry(lastModel);
            }
        }

        /// <inheritdoc/>
        public override void ViewDidDisappear(bool animated)
        {
            expiryTimer?.Dispose();
            expiryTimer = null;
            base.ViewDidDisappear(animated);
        }

        /// <inheritdoc/>
        public void Render(AppModel model)
        {
            lastModel = model;
            if (!IsViewLoaded)
            {
                // Unsichtbare Tabs laden ihre Ansicht nicht
                // vorsorglich — der Stand wird beim ersten ViewDidLoad
                // nachgezogen.
                return;
            }

            ApplyRender(model);
        }

        private void ApplyRender(AppModel model)
        {
            offlineBanner!.Hidden = !model.IsOffline;
            noticeBanner!.Hidden = model.StorageNotice is null;
            noticeBanner.Text = model.StorageNotice ?? string.Empty;

            var desired = BuildSections(model);
            var keys = desired.Select(d => d.Key).ToArray();
            if (!keys.SequenceEqual(appliedKeys))
            {
                RebuildSections(desired);
            }

            for (var index = 0; index < sections.Count; index++)
            {
                sections[index].SetItems(desired[index].Items);
            }

            ScheduleExpiry(model);
        }

        // Aktuelle Sektions-Deskriptoren des Modells (Reihenfolge wie auf der MAUI-Startseite; leere Listen entfallen).
        private static List<HomeSectionDescriptor> BuildSections(AppModel model)
        {
            var desired = new List<HomeSectionDescriptor>();
            if (model.ContinueWatching.Count > 0)
            {
                desired.Add(new HomeSectionDescriptor(
                    "continue", "Weiterschauen", model.ContinueWatching,
                    ContinueWatchingItemSize, IsContinueWatching: true, "ContinueWatchingCarousel"));
            }

            if (model.Favorites.Count > 0)
            {
                desired.Add(new HomeSectionDescriptor(
                    "favorites", "Favoriten", model.Favorites,
                    PosterItemSize, IsContinueWatching: false, "FavoritesCarousel"));
            }

            if (model.RecentEntries.Count > 0)
            {
                desired.Add(new HomeSectionDescriptor(
                    "recent", "Neu im Programm", model.RecentEntries,
                    PosterItemSize, IsContinueWatching: false, "RecentCarousel"));
            }

            foreach (var section in model.SeasonalGenres)
            {
                if (section.Items.Count > 0)
                {
                    desired.Add(new HomeSectionDescriptor(
                        $"seasonal:{section.GenreId}", section.Name, section.Items,
                        PosterItemSize, IsContinueWatching: false, section.AutomationId));
                }
            }

            return desired;
        }

        // Baut die Karussell-Reihen nach einem Wechsel der Sektions-Zusammensetzung neu auf.
        private void RebuildSections(List<HomeSectionDescriptor> desired)
        {
            foreach (var section in sections)
            {
                content!.RemoveArrangedSubview(section);
                section.RemoveFromSuperview();
            }

            sections.Clear();
            foreach (var descriptor in desired)
            {
                var carousel = new HomeCarouselView(
                    descriptor.Title,
                    descriptor.ItemSize,
                    posters,
                    item => OnItemSelected(descriptor, item),
                    descriptor.AccessibilityId);
                sections.Add(carousel);
                content!.AddArrangedSubview(carousel);
            }

            appliedKeys = desired.Select(d => d.Key).ToArray();
        }

        // Auswahl eines Karten-Eintrags: Weiterschauen-Einträge lösen den Fortsetzungs-/Wiedergabepfad aus, alle übrigen die Detail-Navigation — Update entscheidet über Effekte und Platzhalter-Auswahl.
        private void OnItemSelected(HomeSectionDescriptor section, HomeMediaItem item)
        {
            if (!item.IsSelectable)
            {
                // Platzhalter fehlender Offline-Episoden tragen keine
                // serverseitige Identität — bleiben fokussierbar, aber
                // ohne Aktion (Guard-Pendant zu AppUpdate).
                return;
            }

            _ = dispatch(section.IsContinueWatching
                ? new ContinueWatchingItemSelectedMessage(item)
                : new MediaItemSelectedMessage(item));
        }

        // Plant den Ablauf der nächsten saisonalen Sektion: erreicht die
        // serverseitige Ablaufgrenze, wird ein Nachladen der Listen
        // angestoßen (Muster: ScheduleExpiry der MAUI-Startseite;
        // lange Laufzeiten werden gekappt, damit auch Korrekturen der
        // Systemzeit erfasst werden).
        private void ScheduleExpiry(AppModel model)
        {
            expiryTimer?.Dispose();
            expiryTimer = null;
            if (model.SeasonalGenres.Count == 0)
            {
                return;
            }

            var delay = model.SeasonalGenres.Min(g => g.ExpiresAt) - timeProvider.GetUtcNow();
            delay = delay <= TimeSpan.Zero
                ? TimeSpan.FromMilliseconds(1)
                : TimeSpan.FromTicks(Math.Min(delay.Ticks, TimeSpan.FromMinutes(1).Ticks));
            expiryTimer = timeProvider.CreateTimer(
                _ => MainThreadInvoker.InvokeOnMainThread(OnExpiryTick),
                null,
                delay,
                Timeout.InfiniteTimeSpan);
        }

        private void OnExpiryTick()
        {
            var model = lastModel;
            if (model is null || model.SeasonalGenres.Count == 0)
            {
                return;
            }

            if (model.SeasonalGenres.Any(g => g.ExpiresAt <= timeProvider.GetUtcNow()))
            {
                _ = dispatch(new HomeListsReloadRequestedMessage());
            }
            else
            {
                ScheduleExpiry(model);
            }
        }

        // Beschreibung einer Karussell-Reihe aus dem aktuellen Modellstand.
        private sealed record HomeSectionDescriptor(
            string Key,
            string Title,
            IReadOnlyList<HomeMediaItem> Items,
            CGSize ItemSize,
            bool IsContinueWatching,
            string? AccessibilityId);
    }
}
