using CoreGraphics;
using ObjCRuntime;
using UIKit;
using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Fokussierbare Genre-Kachel der Filterleiste der Browsing-Seite:
    /// Text-Chip mit sichtbarem Auswahlzustand — der aktive Filter
    /// (<see cref="SourceGenreItem.IsSelected"/>) erscheint als gefüllte
    /// helle Kachel, übrige Genres als gerahmte Kacheln; die Fokussierung
    /// der Siri Remote vergrößert die Kachel zusätzlich.
    /// </summary>
    public sealed class SourceGenreCell : UICollectionViewCell
    {
        /// <summary>Wiederverwendungs-Kennung der Zelle für <c>RegisterClassForCell</c>/<c>DequeueReusableCell</c> (als <see cref="string"/> — an beide Binding-Varianten <c>string</c>/<c>NSString</c> implizit konvertierbar).</summary>
        public const string ReuseId = "SourceGenreCell";

        private readonly UILabel nameLabel;
        private bool appliedSelected;

        /// <summary>Initialisiert eine neue Instanz der <see cref="SourceGenreCell"/>-Klasse (Registrierungs-Konstruktor für <c>RegisterClassForCell</c>).</summary>
        /// <param name="handle">Das native ObjC-Handle der Zelle.</param>
        public SourceGenreCell(NativeHandle handle)
            : base(handle)
        {
            nameLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(26),
                TextAlignment = UITextAlignment.Center,
                Lines = 1,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            ContentView.BackgroundColor = UIColor.FromWhiteAlpha(0.14f, 1f);
            ContentView.Layer.CornerRadius = 18;
            ContentView.Layer.BorderWidth = 2;
            ContentView.Layer.BorderColor = UIColor.FromWhiteAlpha(1f, 0.25f).CGColor;
            ContentView.AddSubview(nameLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                nameLabel.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 28),
                nameLabel.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -28),
                nameLabel.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor)
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
                        ? CGAffineTransform.MakeScale(1.1f, 1.1f)
                        : CGAffineTransform.MakeIdentity();
                    ApplyAppearance(focused);
                },
                () => { });
        }

        /// <inheritdoc/>
        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            appliedSelected = false;
            nameLabel.Text = string.Empty;
            Transform = CGAffineTransform.MakeIdentity();
            ApplyAppearance(false);
        }

        /// <summary>Belegt die Kachel mit einem Genre-Eintrag samt Auswahlzustand.</summary>
        /// <param name="genre">Der anzuzeigende Genre-Eintrag.</param>
        public void Update(SourceGenreItem genre)
        {
            nameLabel.Text = genre.Name;
            appliedSelected = genre.IsSelected;
            ApplyAppearance(false);
        }

        // Auswahl- und Fokusdarstellung: aktiver Filter = gefüllte helle
        // Kachel; Fokus = hellerer Rahmen/Text.
        private void ApplyAppearance(bool focused)
        {
            if (appliedSelected)
            {
                ContentView.BackgroundColor = UIColor.White;
                nameLabel.TextColor = UIColor.Black;
            }
            else
            {
                ContentView.BackgroundColor = UIColor.FromWhiteAlpha(0.14f, 1f);
                nameLabel.TextColor = focused ? UIColor.White : UIColor.LightGray;
            }

            ContentView.Layer.BorderColor = focused || appliedSelected
                ? UIColor.White.CGColor
                : UIColor.FromWhiteAlpha(1f, 0.25f).CGColor;
        }
    }
}
