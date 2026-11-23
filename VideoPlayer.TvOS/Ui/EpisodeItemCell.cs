using CoreGraphics;
using ObjCRuntime;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Fokussierbare Episodenkarte der Detailseite: 16:9-Still
    /// (<see cref="EpisodeItem.PosterPictureId"/>, asynchron über
    /// <see cref="TvosPosterImageService"/> nachgeladen, Titel als
    /// Fallback), Episoden-Titelzeile, Gesehen-Badge und Hinweis auf
    /// eine lokal vorhandene Datei. Die navigierte Episode
    /// (<see cref="EpisodeItem.IsHighlighted"/>) trägt einen hellen
    /// Rahmen; Platzhalter fehlender Offline-Episoden
    /// (<see cref="EpisodeItem.IsPlaceholder"/>) erscheinen abgedunkelt
    /// mit dem Hinweis „Nicht heruntergeladen" und sind nicht
    /// auswählbar.
    /// </summary>
    public sealed class EpisodeItemCell : UICollectionViewCell
    {
        /// <summary>Wiederverwendungs-Kennung der Zelle für <c>RegisterClassForCell</c>/<c>DequeueReusableCell</c> (als <see cref="string"/> — an beide Binding-Varianten <c>string</c>/<c>NSString</c> implizit konvertierbar).</summary>
        public const string ReuseId = "EpisodeItemCell";

        private static readonly nfloat TitleAreaHeight = 60f;

        private readonly UIImageView stillView;
        private readonly UILabel fallbackLabel;
        private readonly UILabel watchedBadge;
        private readonly UILabel localBadge;
        private readonly UILabel titleLabel;
        private long? appliedPictureId;

        /// <summary>Initialisiert eine neue Instanz der <see cref="EpisodeItemCell"/>-Klasse (Registrierungs-Konstruktor für <c>RegisterClassForCell</c>).</summary>
        /// <param name="handle">Das native ObjC-Handle der Zelle.</param>
        public EpisodeItemCell(NativeHandle handle)
            : base(handle)
        {
            stillView = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFill,
                ClipsToBounds = true,
                BackgroundColor = UIColor.FromWhiteAlpha(0.14f, 1f),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stillView.Layer.CornerRadius = 12;

            fallbackLabel = new UILabel
            {
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(24),
                TextAlignment = UITextAlignment.Center,
                Lines = 3,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            stillView.AddSubview(fallbackLabel);

            watchedBadge = new UILabel
            {
                Text = "✓ Gesehen",
                TextColor = UIColor.Black,
                Font = UIFont.BoldSystemFontOfSize(20),
                TextAlignment = UITextAlignment.Center,
                BackgroundColor = UIColor.FromWhiteAlpha(1f, 0.85f),
                Hidden = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            watchedBadge.Layer.CornerRadius = 10;
            watchedBadge.ClipsToBounds = true;
            stillView.AddSubview(watchedBadge);

            localBadge = new UILabel
            {
                Text = "Auf diesem Gerät",
                TextColor = UIColor.Black,
                Font = UIFont.BoldSystemFontOfSize(18),
                TextAlignment = UITextAlignment.Center,
                BackgroundColor = UIColor.FromWhiteAlpha(0.8f, 0.9f),
                Hidden = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            localBadge.Layer.CornerRadius = 10;
            localBadge.ClipsToBounds = true;
            stillView.AddSubview(localBadge);

            titleLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(24),
                Lines = 2,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            ContentView.AddSubview(stillView);
            ContentView.AddSubview(titleLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                stillView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                stillView.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                stillView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                stillView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -TitleAreaHeight),

                fallbackLabel.CenterXAnchor.ConstraintEqualTo(stillView.CenterXAnchor),
                fallbackLabel.CenterYAnchor.ConstraintEqualTo(stillView.CenterYAnchor),
                fallbackLabel.WidthAnchor.ConstraintLessThanOrEqualTo(stillView.WidthAnchor, 0.85f),

                watchedBadge.TrailingAnchor.ConstraintEqualTo(stillView.TrailingAnchor, -10),
                watchedBadge.TopAnchor.ConstraintEqualTo(stillView.TopAnchor, 10),
                watchedBadge.WidthAnchor.ConstraintGreaterThanOrEqualTo(120),
                watchedBadge.HeightAnchor.ConstraintEqualTo(32),

                localBadge.LeadingAnchor.ConstraintEqualTo(stillView.LeadingAnchor, 10),
                localBadge.BottomAnchor.ConstraintEqualTo(stillView.BottomAnchor, -10),
                localBadge.WidthAnchor.ConstraintGreaterThanOrEqualTo(180),
                localBadge.HeightAnchor.ConstraintEqualTo(32),

                titleLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 8),
                titleLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -8),
                titleLabel.TopAnchor.ConstraintEqualTo(stillView.BottomAnchor, 8)
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
                        ? CGAffineTransform.MakeScale(1.05f, 1.05f)
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
            stillView.Image = null;
            fallbackLabel.Text = string.Empty;
            fallbackLabel.Hidden = false;
            watchedBadge.Hidden = true;
            localBadge.Hidden = true;
            ContentView.Alpha = 1;
            stillView.Layer.BorderWidth = 0;
            Transform = CGAffineTransform.MakeIdentity();
            titleLabel.TextColor = UIColor.LightGray;
        }

        /// <summary>Belegt die Karte mit einem Episoden-Eintrag und stößt ggf. den asynchronen Still-Abruf an.</summary>
        /// <param name="episode">Der anzuzeigende Episoden-Eintrag.</param>
        /// <param name="posters">Der Poster-Cache-/Ladedienst.</param>
        public void Update(EpisodeItem episode, TvosPosterImageService posters)
        {
            titleLabel.Text = episode.IsPlaceholder
                ? $"{episode.DisplayTitle} — nicht heruntergeladen"
                : episode.DisplayTitle;
            fallbackLabel.Text = episode.DisplayTitle;
            ContentView.Alpha = (nfloat)(episode.IsPlaceholder ? 0.55 : 1.0);

            watchedBadge.Hidden = !episode.IsWatched;
            localBadge.Hidden = !episode.HasLocalFile;
            stillView.Layer.BorderWidth = episode.IsHighlighted ? 3 : 0;
            stillView.Layer.BorderColor = episode.IsHighlighted
                ? UIColor.White.CGColor
                : null;

            appliedPictureId = episode.PosterPictureId;
            stillView.Image = null;
            fallbackLabel.Hidden = false;
            if (episode.PosterPictureId is > 0)
            {
                _ = LoadStillAsync(episode.PosterPictureId.Value, posters);
            }
        }

        private async Task LoadStillAsync(long pictureId, TvosPosterImageService posters)
        {
            var image = await posters.GetImageAsync(pictureId);
            if (image is null || appliedPictureId != pictureId)
            {
                // Verworfenes Ergebnis einer bereits recycelten Zelle oder
                // kein Bild verfügbar — der Titel bleibt als Fallback.
                return;
            }

            stillView.Image = image;
            fallbackLabel.Hidden = true;
        }
    }
}
