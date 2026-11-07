using CoreGraphics;
using ObjCRuntime;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Fokussierbare Medienkarte der Startseiten-Karussells: Poster
    /// (asynchron über <see cref="TvosPosterImageService"/> nachgeladen,
    /// bei fehlendem Bild ein Titel-Platzhalter), Titelzeile und
    /// Fortschrittsbalken der Weiterschauen-Einträge. Die
    /// tvOS-Focus-Engine vergrößert die Karte bei Fokussierung;
    /// Platzhalter-Einträge fehlender Offline-Episoden erscheinen
    /// abgedunkelt (<see cref="HomeMediaItem.CardOpacity"/>).
    /// </summary>
    public sealed class HomeMediaItemCell : UICollectionViewCell
    {
        /// <summary>Wiederverwendungs-Kennung der Zelle für <c>RegisterClassForCell</c>/<c>DequeueReusableCell</c> (als <see cref="string"/> — an beide Binding-Varianten <c>string</c>/<c>NSString</c> implizit konvertierbar).</summary>
        public const string ReuseId = "HomeMediaItemCell";

        private static readonly nfloat TitleAreaHeight = 64f;

        private readonly UIImageView posterView;
        private readonly UILabel fallbackLabel;
        private readonly UILabel titleLabel;
        private readonly UIProgressView progressView;
        private long? appliedPictureId;

        /// <summary>Initialisiert eine neue Instanz der <see cref="HomeMediaItemCell"/>-Klasse (Registrierungs-Konstruktor für <c>RegisterClassForCells</c>).</summary>
        /// <param name="handle">Das native ObjC-Handle der Zelle.</param>
        public HomeMediaItemCell(NativeHandle handle)
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

            progressView = new UIProgressView(UIProgressViewStyle.Bar)
            {
                ProgressTintColor = UIColor.SystemRed,
                TrackTintColor = UIColor.FromWhiteAlpha(1f, 0.3f),
                Hidden = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            posterView.AddSubview(progressView);

            titleLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(26),
                Lines = 2,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            ContentView.AddSubview(posterView);
            ContentView.AddSubview(titleLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                posterView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                posterView.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                posterView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                posterView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -TitleAreaHeight),

                fallbackLabel.CenterXAnchor.ConstraintEqualTo(posterView.CenterXAnchor),
                fallbackLabel.CenterYAnchor.ConstraintEqualTo(posterView.CenterYAnchor),
                fallbackLabel.WidthAnchor.ConstraintLessThanOrEqualTo(posterView.WidthAnchor, 0.85f),

                progressView.LeadingAnchor.ConstraintEqualTo(posterView.LeadingAnchor, 12),
                progressView.TrailingAnchor.ConstraintEqualTo(posterView.TrailingAnchor, -12),
                progressView.BottomAnchor.ConstraintEqualTo(posterView.BottomAnchor, -12),

                titleLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 8),
                titleLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -8),
                titleLabel.TopAnchor.ConstraintEqualTo(posterView.BottomAnchor, 10)
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
            progressView.Hidden = true;
            progressView.Progress = 0;
            ContentView.Alpha = 1;
            Transform = CGAffineTransform.MakeIdentity();
            titleLabel.TextColor = UIColor.LightGray;
        }

        /// <summary>Belegt die Karte mit einem Listeneintrag und stößt ggf. den asynchronen Posterabruf an.</summary>
        /// <param name="item">Der anzuzeigende Listeneintrag.</param>
        /// <param name="posters">Der Poster-Cache-/Ladedienst.</param>
        public void Update(HomeMediaItem item, TvosPosterImageService posters)
        {
            titleLabel.Text = item.Title;
            fallbackLabel.Text = item.Title;
            ContentView.Alpha = (nfloat)item.CardOpacity;

            progressView.Hidden = !item.ShowProgress;
            progressView.Progress = (float)(item.Progress ?? 0);

            appliedPictureId = item.PosterPictureId;
            posterView.Image = null;
            fallbackLabel.Hidden = false;
            if (item.PosterPictureId is > 0)
            {
                _ = LoadPosterAsync(item.PosterPictureId.Value, posters);
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
