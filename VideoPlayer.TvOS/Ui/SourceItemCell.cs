using CoreGraphics;
using ObjCRuntime;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Fokussierbare Quellen-Karte der „Quellen"-Reihe der Startseite:
    /// Server-Icon (asynchron über
    /// <see cref="TvosPosterImageService.GetSourceIconAsync"/>
    /// nachgeladen, bei fehlendem Bild der Quellenname als
    /// Titel-Platzhalter) plus Namenszeile. Die tvOS-Focus-Engine
    /// vergrößert die Karte bei Fokussierung — Muster:
    /// <see cref="HomeMediaItemCell"/>.
    /// </summary>
    public sealed class SourceItemCell : UICollectionViewCell
    {
        /// <summary>Wiederverwendungs-Kennung der Zelle für <c>RegisterClassForCell</c>/<c>DequeueReusableCell</c> (als <see cref="string"/> — an beide Binding-Varianten <c>string</c>/<c>NSString</c> implizit konvertierbar).</summary>
        public const string ReuseId = "SourceItemCell";

        private static readonly nfloat TitleAreaHeight = 56f;

        private readonly UIImageView iconView;
        private readonly UILabel fallbackLabel;
        private readonly UILabel nameLabel;
        private long? appliedPictureId;

        /// <summary>Initialisiert eine neue Instanz der <see cref="SourceItemCell"/>-Klasse (Registrierungs-Konstruktor für <c>RegisterClassForCell</c>).</summary>
        /// <param name="handle">Das native ObjC-Handle der Zelle.</param>
        public SourceItemCell(NativeHandle handle)
            : base(handle)
        {
            iconView = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFit,
                ClipsToBounds = true,
                BackgroundColor = UIColor.FromWhiteAlpha(0.14f, 1f),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            iconView.Layer.CornerRadius = 16;

            fallbackLabel = new UILabel
            {
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(26),
                TextAlignment = UITextAlignment.Center,
                Lines = 3,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            iconView.AddSubview(fallbackLabel);

            nameLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(26),
                Lines = 1,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            ContentView.AddSubview(iconView);
            ContentView.AddSubview(nameLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                iconView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                iconView.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                iconView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                iconView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor, -TitleAreaHeight),

                fallbackLabel.CenterXAnchor.ConstraintEqualTo(iconView.CenterXAnchor),
                fallbackLabel.CenterYAnchor.ConstraintEqualTo(iconView.CenterYAnchor),
                fallbackLabel.WidthAnchor.ConstraintLessThanOrEqualTo(iconView.WidthAnchor, 0.85f),

                nameLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 8),
                nameLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -8),
                nameLabel.TopAnchor.ConstraintEqualTo(iconView.BottomAnchor, 10)
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
                    nameLabel.TextColor = focused ? UIColor.White : UIColor.LightGray;
                },
                () => { });
        }

        /// <inheritdoc/>
        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            appliedPictureId = null;
            iconView.Image = null;
            fallbackLabel.Text = string.Empty;
            fallbackLabel.Hidden = false;
            Transform = CGAffineTransform.MakeIdentity();
            nameLabel.TextColor = UIColor.LightGray;
        }

        /// <summary>Belegt die Karte mit einer Quelle und stößt ggf. den asynchronen Icon-Abruf an.</summary>
        /// <param name="item">Der anzuzeigende Quelleneintrag.</param>
        /// <param name="posters">Der Bild-Cache-/Ladedienst.</param>
        public void Update(SourceItem item, TvosPosterImageService posters)
        {
            nameLabel.Text = item.Name;
            fallbackLabel.Text = item.Name;

            appliedPictureId = item.IconPictureId;
            iconView.Image = null;
            fallbackLabel.Hidden = false;
            if (item.IconPictureId is > 0)
            {
                _ = LoadIconAsync(item.IconPictureId.Value, posters);
            }
        }

        private async Task LoadIconAsync(long pictureId, TvosPosterImageService posters)
        {
            var image = await posters.GetSourceIconAsync(pictureId);
            if (image is null || appliedPictureId != pictureId)
            {
                // Verworfenes Ergebnis einer bereits recycelten Zelle oder
                // kein Icon verfügbar — der Name bleibt als Fallback.
                return;
            }

            iconView.Image = image;
            fallbackLabel.Hidden = true;
        }
    }
}
