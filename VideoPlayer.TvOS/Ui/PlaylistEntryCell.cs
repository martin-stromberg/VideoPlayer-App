using CoreGraphics;
using ObjCRuntime;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Fokussierbare Eintragskarte der Playlist-Detailseite: 2:3-Poster
    /// (<see cref="PlaylistEntryItem.ResolvedPictureId"/>, asynchron über
    /// <see cref="TvosPosterImageService"/> nachgeladen, Titel als
    /// Fallback), Titelzeile und Untertitel mit dem übergeordneten
    /// Kontext (z. B. „Serie · Episode N"). Nicht zugängliche Einträge
    /// (<see cref="PlaylistEntryItem.IsAccessible"/>) erscheinen
    /// abgedunkelt.
    /// </summary>
    public sealed class PlaylistEntryCell : UICollectionViewCell
    {
        /// <summary>Wiederverwendungs-Kennung der Zelle für <c>RegisterClassForCell</c>/<c>DequeueReusableCell</c> (als <see cref="string"/> — an beide Binding-Varianten <c>string</c>/<c>NSString</c> implizit konvertierbar).</summary>
        public const string ReuseId = "PlaylistEntryCell";

        private static readonly nfloat TitleAreaHeight = 110f;

        private readonly UIImageView posterView;
        private readonly UILabel fallbackLabel;
        private readonly UILabel titleLabel;
        private readonly UILabel subtitleLabel;
        private long? appliedPictureId;

        /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistEntryCell"/>-Klasse (Registrierungs-Konstruktor für <c>RegisterClassForCell</c>).</summary>
        /// <param name="handle">Das native ObjC-Handle der Zelle.</param>
        public PlaylistEntryCell(NativeHandle handle)
            : base(handle)
        {
            posterView = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFill,
                ClipsToBounds = true,
                BackgroundColor = UIColor.FromWhiteAlpha(0.14f, 1f),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            posterView.Layer.CornerRadius = 12;

            fallbackLabel = new UILabel
            {
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(24),
                TextAlignment = UITextAlignment.Center,
                Lines = 3,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            posterView.AddSubview(fallbackLabel);

            titleLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(26),
                Lines = 2,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            subtitleLabel = new UILabel
            {
                TextColor = UIColor.DarkGray,
                Font = UIFont.SystemFontOfSize(22),
                Lines = 1,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            ContentView.AddSubview(posterView);
            ContentView.AddSubview(titleLabel);
            ContentView.AddSubview(subtitleLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                posterView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                posterView.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                posterView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                posterView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -TitleAreaHeight),

                fallbackLabel.CenterXAnchor.ConstraintEqualTo(posterView.CenterXAnchor),
                fallbackLabel.CenterYAnchor.ConstraintEqualTo(posterView.CenterYAnchor),
                fallbackLabel.WidthAnchor.ConstraintLessThanOrEqualTo(posterView.WidthAnchor, 0.85f),

                titleLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 8),
                titleLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -8),
                titleLabel.TopAnchor.ConstraintEqualTo(posterView.BottomAnchor, 10),

                subtitleLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 8),
                subtitleLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -8),
                subtitleLabel.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, 6)
            ]);
        }

        /// <inheritdoc/>
        public override bool CanBecomeFocused => true;

        /// <inheritdoc/>
        public override void DidUpdateFocus(UIFocusUpdateContext context, UIFocusAnimationCoordinator focusAnimationCoordinator)
        {
            base.DidUpdateFocus(context, focusAnimationCoordinator);
            var focused = ReferenceEquals(context.NextFocusedView, this);
            focusAnimationCoordinator.AddCoordinatedAnimations(
                () =>
                {
                    Transform = focused
                        ? CGAffineTransform.MakeScale(1.06f, 1.06f)
                        : CGAffineTransform.MakeIdentity();
                    titleLabel.TextColor = focused ? UIColor.White : UIColor.LightGray;
                },
                () => { });
        }

        /// <inheritdoc/>
        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            appliedPictureId = null;
            posterView.Image = null;
            fallbackLabel.Text = string.Empty;
            fallbackLabel.Hidden = false;
            titleLabel.Text = string.Empty;
            subtitleLabel.Text = string.Empty;
            ContentView.Alpha = 1;
            Transform = CGAffineTransform.MakeIdentity();
            titleLabel.TextColor = UIColor.LightGray;
        }

        /// <summary>Belegt die Karte mit einem Playlist-Eintrag und stößt ggf. den asynchronen Posterabruf an.</summary>
        /// <param name="entry">Der anzuzeigende Playlist-Eintrag.</param>
        /// <param name="posters">Der Poster-Cache-/Ladedienst.</param>
        public void Update(PlaylistEntryItem entry, TvosPosterImageService posters)
        {
            titleLabel.Text = entry.Title;
            subtitleLabel.Text = entry.IsAccessible ? entry.Subtitle : "Kein Zugriff";
            fallbackLabel.Text = entry.Title;
            ContentView.Alpha = (nfloat)(entry.IsAccessible ? 1.0 : 0.55);

            appliedPictureId = entry.ResolvedPictureId;
            posterView.Image = null;
            fallbackLabel.Hidden = false;
            if (entry.ResolvedPictureId is > 0)
            {
                _ = LoadPosterAsync(entry.ResolvedPictureId.Value, posters);
            }
        }

        private async Task LoadPosterAsync(long pictureId, TvosPosterImageService posters)
        {
            var image = await posters.GetImageAsync(pictureId);
            if (image is null || appliedPictureId != pictureId)
            {
                // Verworfenes Ergebnis einer bereits recycelten Zelle oder
                // kein Bild verfügbar — der Titel bleibt als Fallback.
                return;
            }

            posterView.Image = image;
            fallbackLabel.Hidden = true;
        }
    }
}
