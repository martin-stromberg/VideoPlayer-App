using AVFoundation;
using AVKit;
using CoreFoundation;
using CoreMedia;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Wiedergabe-Seite der tvOS-App — nativer Vollbild-Player auf Basis
    /// des <see cref="AVPlayerViewController"/> mit systemeigenem
    /// Transport-UI. Der Controller ist ein reiner Ereignis-Übersetzer in
    /// den MVU-Zyklus (Pendant zur MAUI-<c>PlayerPage</c>): die aufgelöste
    /// Quelle des <see cref="PlayerState"/> wird auf den
    /// <see cref="AVPlayer"/> angewendet, die Fortsetzungsposition wird
    /// bei Wiedergabebereitschaft angesprungen und ein periodischer
    /// Zeitcobserver meldet den Fortschritt im 5-Sekunden-Takt als
    /// <see cref="PlaybackProgressMessage"/> — Takt-, Schwellen- und
    /// Fortsetzungslogik verbleiben im plattformneutralen
    /// <c>AppUpdate</c>. Ende- und Fehlerereignisse des Items werden als
    /// <see cref="PlaybackEndedMessage"/>/<see cref="PlaybackFailedMessage"/>
    /// gemeldet (die Toleranz-Plausibilisierung des Endes liegt im
    /// MVU-Kern). Playlist-Navigation (next/previous) liegt als
    /// <see cref="AVPlayerViewController.TransportBarCustomMenuItems"/> in
    /// der Transport-Bar; Titel/Playlist-Kontext zeigt der
    /// <see cref="TvosPlayerInfoViewController"/>-Tab im Pull-down-
    /// Infobereich (<see cref="AVPlayerViewController.CustomInfoViewControllers"/>),
    /// der zugleich die systemeigene Spurwahl enthält — ohne auswählbare
    /// Spurgruppen des Streams entfällt die Auswahl ersatzlos.
    /// </summary>
    public sealed class TvosPlayerViewController : AVPlayerViewController, IModelRenderable
    {
        private static readonly TimeSpan ProgressTickInterval = TimeSpan.FromSeconds(5);

        private readonly Func<AppMessage, Task> dispatch;
        private readonly Action<TvosPlayerViewController> onDismissed;
        private readonly ILogger<TvosPlayerViewController> logger;
        private readonly TvosPlayerInfoViewController infoPage = new();

        private UIActivityIndicatorView? resolvingIndicator;
        private bool errorAlertShown;

        private string? appliedLocation;
        // Zuletzt am Player angewendete MediaId — der Fortschrittstakt
        // meldet sie mit, damit der MVU-Kern nachlaufende Takte des
        // Vorgängermediums verwerfen kann (Muster appliedMediaId der
        // MAUI-Seite); 0 = noch keine Quelle angewendet.
        private long appliedMediaId;
        // Erzwingt den Setz-/Leerpfad nach einem fehlgeschlagenen
        // Quellenwechsel (Muster sourceApplyFailed der MAUI-Seite).
        private bool sourceApplyFailed;
        // Gemerkter Fortsetzungs-Seek der aktuellen Quelle — wird erst bei
        // ReadyToPlay angewendet (Zugriff nur Hauptthread).
        private TimeSpan? pendingResumePosition;

        private NSObject? periodicObserver;
        private IDisposable? itemStatusObserver;
        private IDisposable? timeControlObserver;
        private NSObject? didEndObserver;
        private NSObject? failedEndObserver;
        private NSObject? stalledObserver;

        private bool opened;
        private bool dismissed;

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosPlayerViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="onDismissed">Freigabe-Callback des Navigators beim Schließen des Players.</param>
        /// <param name="logger">Logger für das Programmprotokoll.</param>
        public TvosPlayerViewController(
            Func<AppMessage, Task> dispatch,
            Action<TvosPlayerViewController> onDismissed,
            ILogger<TvosPlayerViewController> logger)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.onDismissed = onDismissed ?? throw new ArgumentNullException(nameof(onDismissed));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Player = new AVPlayer();
            // System-Spurwahl: der Pull-down-Infobereich des
            // AVPlayerViewController zeigt Ton-/Untertitel-Auswahl
            // automatisch, wenn der Stream AVMediaSelectionGroups
            // anbietet (z. B. HLS) — ohne Gruppen entfällt sie.
            CustomInfoViewControllers = [infoPage];

            resolvingIndicator = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Large)
            {
                HidesWhenStopped = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            if (ContentOverlayView is { } overlay)
            {
                overlay.AddSubview(resolvingIndicator);
                NSLayoutConstraint.ActivateConstraints(
                [
                    resolvingIndicator.CenterXAnchor.ConstraintEqualTo(overlay.CenterXAnchor),
                    resolvingIndicator.CenterYAnchor.ConstraintEqualTo(overlay.CenterYAnchor)
                ]);
            }

            // Fortschrittstakt (Pendant zum 5-s-Timer der MAUI-Seite):
            // der Observer läuft auf der Haupt-Queue — UIKit- und
            // Dispatch-Zugriffe sind hauptthread-gebunden.
            periodicObserver = Player.AddPeriodicTimeObserver(
                CMTime.FromSeconds(ProgressTickInterval.TotalSeconds, 1),
                DispatchQueue.MainQueue,
                _ => ReportPosition(onlyWhilePlaying: true));

            // Zustandswechsel (Pause, Pufferung, Wiedergabe) sofort in
            // den MVU-Zyklus spiegeln (Muster StateChanged der MAUI-Seite).
            timeControlObserver = Player.AddObserver(
                "timeControlStatus",
                NSKeyValueObservingOptions.New,
                _ => ReportPosition(onlyWhilePlaying: false));
        }

        /// <inheritdoc/>
        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            if (!opened)
            {
                opened = true;
                _ = dispatch(new PlayerPageOpenedMessage());
            }
        }

        /// <inheritdoc/>
        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            var closing = IsBeingDismissed
                || IsMovingFromParentViewController
                || (PresentingViewController?.IsBeingDismissed ?? false);
            if (!closing || dismissed)
            {
                return;
            }

            // Verlassene Wiedergabe: Observer abbauen, Abschluss-
            // Fortschritt melden und den Player-Zustand leeren
            // (Pendant OnDisappearing der MAUI-Seite).
            dismissed = true;
            DetachItemObservers(Player?.CurrentItem);
            DetachPlayerObservers();
            Player?.Pause();
            Player?.ReplaceCurrentItemWithPlayerItem(null);
            _ = dispatch(new PlayerClosedMessage());
            onDismissed(this);
        }

        /// <inheritdoc/>
        public void Render(AppModel model)
        {
            if (!IsViewLoaded)
            {
                return;
            }

            var player = model.Player;
            infoPage.Update(player);
            ApplyTransportMenu(player);
            ApplyResolvingState(player);
            ApplyErrorState(player);
            ApplySource(player);
        }

        private void ApplyTransportMenu(PlayerState player)
        {
            if (!player.IsPlaylistPlayback)
            {
                TransportBarCustomMenuItems = [];
                return;
            }

            var previous = UIAction.Create(
                "Vorheriger Eintrag", null, null,
                _ =>
                {
                    _ = dispatch(new PlaylistPreviousRequestedMessage());
                });
            if (!player.CanNavigatePrevious)
            {
                previous.Attributes = UIMenuElementAttributes.Disabled;
            }

            var next = UIAction.Create(
                "Nächster Eintrag", null, null,
                _ =>
                {
                    _ = dispatch(new PlaylistNextRequestedMessage());
                });
            if (!player.CanNavigateNext)
            {
                next.Attributes = UIMenuElementAttributes.Disabled;
            }

            TransportBarCustomMenuItems = [previous, next];
        }

        private void ApplyResolvingState(PlayerState player)
        {
            if (resolvingIndicator is null)
            {
                return;
            }

            if (player.IsResolving)
            {
                resolvingIndicator.Hidden = false;
                resolvingIndicator.StartAnimating();
            }
            else
            {
                resolvingIndicator.StopAnimating();
                resolvingIndicator.Hidden = true;
            }
        }

        private void ApplyErrorState(PlayerState player)
        {
            // Der Fehler wird erst gezeigt, wenn kein Rückfall mehr läuft
            // (IsResolving) — ein überbrückter Fehler der Fortsetzung
            // blockiert die Anzeige nicht (Pendant zum Fehlerpanel).
            if (!player.HasError || player.IsResolving || errorAlertShown || dismissed)
            {
                return;
            }

            errorAlertShown = true;
            var alert = UIAlertController.Create(
                "Wiedergabe",
                player.Error,
                UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create(
                "Schließen",
                UIAlertActionStyle.Default,
                _ => DismissViewController(true, null)));
            PresentViewController(alert, true, null);
        }

        private void ApplySource(PlayerState player)
        {
            var location = player.Source?.Location;
            if (!sourceApplyFailed && location == appliedLocation)
            {
                return;
            }

            sourceApplyFailed = false;
            appliedLocation = location;
            // Fortsetzungsposition der neuen Quelle merken — der Seek wird
            // erst bei ReadyToPlay ausgeführt (OnItemStatusChanged).
            pendingResumePosition = player.Source?.ResumePosition;
            if (location is null || player.Source is null)
            {
                DetachItemObservers(Player?.CurrentItem);
                Player?.Pause();
                Player?.ReplaceCurrentItemWithPlayerItem(null);
                appliedMediaId = 0;
                return;
            }

            var url = CreateSourceUrl(player.Source, location);
            if (url is null)
            {
                // Die Quell-Adresse bleibt unprotokolliert (access_token).
                FailPlaybackStart($"Ungültige Quell-Adresse ({player.Source.Kind}).");
                return;
            }

            try
            {
                var item = AVPlayerItem.FromUrl(url);
                DetachItemObservers(Player?.CurrentItem);
                AttachItemObservers(item);
                // Identität der gemeldeten Takte erst nach dem
                // erfolgreichen Quellenwechsel umschwenken (Muster
                // appliedMediaId der MAUI-Seite).
                Player!.ReplaceCurrentItemWithPlayerItem(item);
                appliedMediaId = player.MediaId;
                Player.Play();
            }
            catch (Exception ex)
            {
                FailPlaybackStart($"Setzen der Quelle fehlgeschlagen: {SanitizeForLog(ex.Message)}");
            }
        }

        /// <summary>
        /// Frühvalidierung und URL-Erzeugung der aufgelösten Quelle:
        /// <see cref="PlaybackSourceKind.ServerStream"/> verlangt eine
        /// absolute http/https-URL, <see cref="PlaybackSourceKind.LocalFile"/>
        /// einen nicht leeren Pfad (Pendant zu IsValidSourceLocation der
        /// MAUI-Seite).
        /// </summary>
        /// <param name="source">Die aufgelöste Quelle (nur die Art ist relevant).</param>
        /// <param name="location">Die zu prüfende Quell-Adresse (URL oder Pfad).</param>
        /// <returns>Die <see cref="NSUrl"/> bzw. <c>null</c> bei ungültiger Adresse.</returns>
        private static NSUrl? CreateSourceUrl(PlaybackSource source, string location)
        {
            return source.Kind switch
            {
                PlaybackSourceKind.ServerStream =>
                    Uri.TryCreate(location, UriKind.Absolute, out var uri)
                    && uri.Scheme is "http" or "https"
                        ? NSUrl.FromString(location)
                        : null,
                PlaybackSourceKind.LocalFile =>
                    string.IsNullOrWhiteSpace(location) ? null : NSUrl.FromFilename(location),
                _ => null
            };
        }

        private void AttachItemObservers(AVPlayerItem? item)
        {
            if (item is null)
            {
                return;
            }

            // Item-Bereitschaft: erst bei ReadyToPlay darf der gemerkte
            // Fortsetzungs-Seek angewendet werden (Seek vor Bereitschaft
            // ist fehleranfällig — MAUI-Muster); Failed → Fehlermeldung.
            itemStatusObserver = item.AddObserver(
                "status",
                NSKeyValueObservingOptions.New,
                _ => OnItemStatusChanged(item));

            didEndObserver = AVPlayerItem.Notifications.ObserveDidPlayToEndTime(
                item,
                (_, _) =>
                {
                    logger.LogInformation("Wiedergabe beendet: {Title}.", CurrentTitleForLog());
                    DispatchIfActive(new PlaybackEndedMessage());
                });
            // Der ObserveFailedToPlayToEndTime-Helfer fehlt im
            // tvOS-Binding — das Ende-Fehlerereignis wird direkt über
            // das NotificationCenter mit dem stabilen
            // AVPlayerItem-Notification-Namen abonniert.
            failedEndObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                new NSString("AVPlayerItemFailedToPlayToEndTimeNotification"),
                item,
                notification =>
                {
                    var error = notification.Object is AVPlayerItem failedItem
                        ? failedItem.Error?.LocalizedDescription
                        : notification.UserInfo?[new NSString("error")] is NSError nsError
                            ? nsError.LocalizedDescription
                            : null;
                    logger.LogError("Wiedergabe fehlgeschlagen: {LogMessage}", SanitizeForLog(error));
                    DispatchIfActive(new PlaybackFailedMessage());
                });
            stalledObserver = AVPlayerItem.Notifications.ObservePlaybackStalled(
                item,
                (_, _) => ReportPosition(onlyWhilePlaying: false));
        }

        private void DetachItemObservers(AVPlayerItem? item)
        {
            itemStatusObserver?.Dispose();
            itemStatusObserver = null;
            didEndObserver?.Dispose();
            didEndObserver = null;
            failedEndObserver?.Dispose();
            failedEndObserver = null;
            stalledObserver?.Dispose();
            stalledObserver = null;
        }

        private void DetachPlayerObservers()
        {
            if (periodicObserver is not null)
            {
                Player?.RemoveTimeObserver(periodicObserver);
                periodicObserver = null;
            }

            timeControlObserver?.Dispose();
            timeControlObserver = null;
        }

        private void OnItemStatusChanged(AVPlayerItem item)
        {
            switch (item.Status)
            {
                case AVPlayerItemStatus.Failed:
                    logger.LogError(
                        "Wiedergabe fehlgeschlagen: {LogMessage}",
                        SanitizeForLog(item.Error?.LocalizedDescription));
                    DispatchIfActive(new PlaybackFailedMessage());
                    break;
                case AVPlayerItemStatus.ReadyToPlay:
                    TryApplyPendingResumeSeek();
                    ReportPosition(onlyWhilePlaying: false);
                    break;
            }
        }

        /// <summary>
        /// Wendet den gemerkten Fortsetzungs-Seek an, sobald das Item
        /// wiedergabebereit ist (Pendant TryApplyPendingResumeSeek der
        /// MAUI-Seite). Die Fortsetzungsposition wird beim Quellwechsel
        /// in <see cref="ApplySource"/> über <see cref="Render"/> gemerkt —
        /// der Seek wird hier ausgeführt, weil die Bereitschaft erst per
        /// KVO eintrifft.
        /// </summary>
        private void TryApplyPendingResumeSeek()
        {
            if (pendingResumePosition is not { } resume || resume <= TimeSpan.Zero)
            {
                return;
            }

            pendingResumePosition = null;
            var target = CMTime.FromSeconds(resume.TotalSeconds, 600);
            Player?.Seek(target, CMTime.Zero, CMTime.Zero, finished =>
            {
                if (finished)
                {
                    logger.LogInformation("Fortsetzungs-Seek auf {Position} angewendet.", resume);
                }
                else
                {
                    // Fehlschlag ist nicht-fatal: die Wiedergabe läuft an
                    // der erreichten Position weiter (kein Rückfall).
                    logger.LogWarning("Fortsetzungs-Seek auf {Position} fehlgeschlagen.", resume);
                }
            });
        }

        private void ReportPosition(bool onlyWhilePlaying)
        {
            try
            {
                var player = Player;
                var item = player?.CurrentItem;
                if (player is null || item is null || dismissed)
                {
                    return;
                }

                var isPlaying = player.TimeControlStatus == AVPlayerTimeControlStatus.Playing;
                if (onlyWhilePlaying && !isPlaying)
                {
                    // MAUI-Muster: der Takt meldet nur während laufender
                    // Wiedergabe — Pause/Pufferung kommen über die
                    // Zustandswechsel-Meldung herein.
                    return;
                }

                var position = ToTimeSpan(item.CurrentTime);
                var duration = ToTimeSpan(item.Duration);
                var isBuffering = player.TimeControlStatus
                    == AVPlayerTimeControlStatus.WaitingToPlayAtSpecifiedRate;

                _ = dispatch(new PlaybackProgressMessage(
                    position,
                    duration,
                    isPlaying,
                    isBuffering,
                    appliedMediaId));
            }
            catch (Exception ex)
            {
                // Ein Fehler am nativen Player darf im Observer-Callback
                // nicht unbehandelt bleiben (nur protokollieren; der
                // nächste Takt versucht es erneut).
                logger.LogWarning(ex, "Positionsmeldung fehlgeschlagen.");
            }
        }

        private static TimeSpan ToTimeSpan(CMTime time)
        {
            if (time.IsInvalid || time.IsIndefinite || double.IsNaN(time.Seconds))
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromSeconds(Math.Max(0, time.Seconds));
        }

        private void FailPlaybackStart(string logMessage)
        {
            logger.LogError("Wiedergabestart fehlgeschlagen: {LogMessage}", logMessage);
            sourceApplyFailed = true;
            DispatchIfActive(new PlaybackFailedMessage());
        }

        private void DispatchIfActive(AppMessage message)
        {
            if (!dismissed)
            {
                _ = dispatch(message);
            }
        }

        private string CurrentTitleForLog() =>
            Player?.CurrentItem is not null ? $"MediaId {appliedMediaId}" : "—";

        /// <summary>
        /// Bereinigt plattform- oder ausnahmegenerierte Meldungen vor dem
        /// Protokollieren: eine enthaltene Quell-URL kann die
        /// access_token-Query tragen — der Token-Wert wird maskiert
        /// (Pendant SanitizeForLog der MAUI-Seite).
        /// </summary>
        /// <param name="message">Die rohe Diagnosemeldung.</param>
        /// <returns>Die Meldung mit maskiertem access_token-Wert.</returns>
        private static string SanitizeForLog(string? message)
        {
            return message is null ? string.Empty : AppLogService.MaskAccessToken(message);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DetachItemObservers(Player?.CurrentItem);
                DetachPlayerObservers();
            }

            base.Dispose(disposing);
        }
    }
}
