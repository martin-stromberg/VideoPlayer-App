using UIKit;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Inhaltsseite eines Menübereichs: Titel und Untertitel als
    /// Vorschau-Kopfzeile — der eigentliche Inhalt entsteht in den
    /// späteren Entwicklungsschritten.
    /// </summary>
    public sealed class PagePlaceholderViewController : UIViewController
    {
        private readonly string subtitle;

        /// <summary>Initialisiert eine neue Instanz der <see cref="PagePlaceholderViewController"/>-Klasse.</summary>
        /// <param name="title">Seitentitel (zugleich Tab-Bezeichnung).</param>
        /// <param name="subtitle">Hinweistext unterhalb des Titels.</param>
        public PagePlaceholderViewController(string title, string subtitle)
        {
            Title = title;
            this.subtitle = subtitle;
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            var titleLabel = new UILabel
            {
                Text = Title,
                TextColor = UIColor.White,
                Font = UIFont.BoldSystemFontOfSize(64),
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var subtitleLabel = new UILabel
            {
                Text = subtitle,
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(30),
                Lines = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            View.AddSubview(titleLabel);
            View.AddSubview(subtitleLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                titleLabel.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                titleLabel.TopAnchor.ConstraintEqualTo(View.TopAnchor, 160),
                subtitleLabel.LeadingAnchor.ConstraintEqualTo(titleLabel.LeadingAnchor),
                subtitleLabel.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, 24),
                subtitleLabel.WidthAnchor.ConstraintLessThanOrEqualTo(View.WidthAnchor, 0.7f)
            ]);
        }
    }
}
