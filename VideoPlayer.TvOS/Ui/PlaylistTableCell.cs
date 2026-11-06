using CoreGraphics;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Listenzeile der Playlists-Übersicht: Cover
    /// (<c>api/playlists/{id}/cover</c>, asynchron über
    /// <see cref="TvosPosterImageService"/> nachgeladen, Name als
    /// Fallback), Name, Sortiermodus und Anzeige-Genres. Die
    /// Fokussierung hebt die Zeile über die tvOS-Focus-Engine hervor.
    /// </summary>
    public sealed class PlaylistTableCell : UITableViewCell
    {
        /// <summary>Wiederverwendungs-Kennung der Zelle für <c>DequeueReusableCell</c>.</summary>
        public const string ReuseId = "PlaylistTableCell";

        private static readonly nfloat CoverWidth = 240f;
        private static readonly nfloat CoverHeight = 135f;

        private readonly UIImageView coverView;
        private readonly UILabel fallbackLabel;
        private readonly UILabel titleLabel;
        private readonly UILabel subtitleLabel;
        private long? appliedPlaylistId;

        /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistTableCell"/>-Klasse.</summary>
        public PlaylistTableCell()
            : base(UITableViewCellStyle.Default, ReuseId)
        {
            BackgroundColor = UIColor.Clear;

            coverView = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFill,
                ClipsToBounds = true,
                BackgroundColor = UIColor.FromWhiteAlpha(0.14f, 1f),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            coverView.Layer.CornerRadius = 10;

            fallbackLabel = new UILabel
            {
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(20),
                TextAlignment = UITextAlignment.Center,
                Lines = 3,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            coverView.AddSubview(fallbackLabel);

            titleLabel = new UILabel
            {
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(30),
                Lines = 2,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            subtitleLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(24),
                Lines = 1,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var textStack = new UIStackView([titleLabel, subtitleLabel])
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Fill,
                Distribution = UIStackViewDistribution.Fill,
                Spacing = 8,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            ContentView.AddSubview(coverView);
            ContentView.AddSubview(textStack);

            NSLayoutConstraint.ActivateConstraints(
            [
                coverView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 30),
                coverView.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor),
                coverView.WidthAnchor.ConstraintEqualTo(CoverWidth),
                coverView.HeightAnchor.ConstraintEqualTo(CoverHeight),

                fallbackLabel.CenterXAnchor.ConstraintEqualTo(coverView.CenterXAnchor),
                fallbackLabel.CenterYAnchor.ConstraintEqualTo(coverView.CenterYAnchor),
                fallbackLabel.WidthAnchor.ConstraintLessThanOrEqualTo(coverView.WidthAnchor, 0.85f),

                textStack.LeadingAnchor.ConstraintEqualTo(coverView.TrailingAnchor, 28),
                textStack.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -30),
                textStack.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor)
            ]);
        }

        /// <inheritdoc/>
        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            appliedPlaylistId = null;
            coverView.Image = null;
            fallbackLabel.Text = string.Empty;
            fallbackLabel.Hidden = false;
            titleLabel.Text = string.Empty;
            subtitleLabel.Text = string.Empty;
        }

        /// <summary>Belegt die Zeile mit einem Playlist-Eintrag und stößt ggf. den asynchronen Cover-Abruf an.</summary>
        /// <param name="playlist">Der anzuzeigende Playlist-Eintrag.</param>
        /// <param name="posters">Der Bild-Cache-/Ladedienst (Cover-Endpunkt).</param>
        public void Update(PlaylistListItem playlist, TvosPosterImageService posters)
        {
            titleLabel.Text = playlist.Name;
            subtitleLabel.Text = playlist.HasGenreText
                ? $"{playlist.SortModeText} · {playlist.GenreText}"
                : playlist.SortModeText;
            fallbackLabel.Text = playlist.Name;
            fallbackLabel.Hidden = false;

            appliedPlaylistId = playlist.CoverPlaylistId;
            coverView.Image = null;
            if (playlist.CoverPlaylistId is > 0)
            {
                _ = LoadCoverAsync(playlist.CoverPlaylistId.Value, posters);
            }
        }

        private async Task LoadCoverAsync(long playlistId, TvosPosterImageService posters)
        {
            var image = await posters.GetPlaylistCoverAsync(playlistId);
            if (image is null || appliedPlaylistId != playlistId)
            {
                // Verworfenes Ergebnis einer bereits recycelten Zelle
                // oder kein Cover verfügbar — der Name bleibt als
                // Fallback.
                return;
            }

            coverView.Image = image;
            fallbackLabel.Hidden = true;
        }
    }
}
