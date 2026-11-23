using UIKit;
using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Info-Tab des Pull-down-Infobereichs des nativen tvOS-Players
    /// (<see cref="AVKit.AVPlayerViewController.CustomInfoViewControllers"/>,
    /// tvOS 15+): zeigt den Titel des wiedergegebenen Mediums, die
    /// Playlist-Kontextzeile (<see cref="PlayerState.PlaylistLabel"/>) und
    /// den „angesehen"-Hinweis — Pendant zur Titel-/Kontextzeile der
    /// MAUI-Wiedergabeseite. Die Inhalte werden modellgetrieben über
    /// <see cref="Update"/> vom Player-Controller gespeist.
    /// </summary>
    public sealed class TvosPlayerInfoViewController : UIViewController
    {
        private UILabel? titleLabel;
        private UILabel? playlistLabel;
        private UILabel? watchedLabel;
        private UILabel? statusLabel;
        private string pendingTitle = string.Empty;
        private string pendingPlaylist = string.Empty;
        private string pendingWatched = string.Empty;
        private string pendingStatus = string.Empty;

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosPlayerInfoViewController"/>-Klasse.</summary>
        public TvosPlayerInfoViewController()
        {
            Title = "Wiedergabe";
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            titleLabel = TvosControls.Heading(pendingTitle);
            titleLabel.Lines = 0;
            playlistLabel = TvosControls.Body(pendingPlaylist);
            watchedLabel = TvosControls.Body(pendingWatched);
            statusLabel = TvosControls.Status();

            var content = TvosControls.Section(
                titleLabel,
                playlistLabel,
                watchedLabel,
                statusLabel);
            content.Spacing = 18;
            View.AddSubview(content);

            NSLayoutConstraint.ActivateConstraints(
            [
                content.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                content.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -90),
                content.TopAnchor.ConstraintEqualTo(View.TopAnchor, 90)
            ]);

            ApplyPending();
        }

        /// <summary>Aktualisiert die Anzeigetexte aus dem Player-Zustand (Aufruf des Player-Controllers bei jeder Modelländerung).</summary>
        /// <param name="player">Der aktuelle Player-Zustand.</param>
        public void Update(PlayerState player)
        {
            pendingTitle = player.Title ?? string.Empty;
            pendingPlaylist = player.IsPlaylistPlayback ? player.PlaylistLabel : string.Empty;
            pendingWatched = player.IsWatched ? "✓ Als gesehen markiert" : string.Empty;
            pendingStatus = player.IsResolving
                ? "Quelle wird aufgelöst…"
                : player.IsBuffering
                    ? "Puffert…"
                    : string.Empty;
            ApplyPending();
        }

        private void ApplyPending()
        {
            if (!IsViewLoaded)
            {
                return;
            }

            titleLabel!.Text = pendingTitle;
            playlistLabel!.Text = pendingPlaylist;
            playlistLabel.Hidden = pendingPlaylist.Length == 0;
            watchedLabel!.Text = pendingWatched;
            watchedLabel.Hidden = pendingWatched.Length == 0;
            statusLabel!.Text = pendingStatus;
        }
    }
}
