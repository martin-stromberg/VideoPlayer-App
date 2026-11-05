using UIKit;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Ladeanzeige des Start-Workflows: Aktivitätsindikator plus
    /// modellgetriebener Statustext (<see cref="VideoPlayer.Mvu.AppModel.StartupStatusText"/>)
    /// und optionaler Ablage-Hinweis (<see cref="VideoPlayer.Mvu.AppModel.StorageNotice"/>).
    /// </summary>
    public sealed class LoadingViewController : UIViewController
    {
        private UIActivityIndicatorView? indicator;
        private UILabel? statusLabel;
        private UILabel? noticeLabel;

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            indicator = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Large)
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            indicator.StartAnimating();

            statusLabel = new UILabel
            {
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(38),
                TextAlignment = UITextAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            noticeLabel = new UILabel
            {
                TextColor = UIColor.SystemOrange,
                Font = UIFont.SystemFontOfSize(28),
                TextAlignment = UITextAlignment.Center,
                Lines = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            View.AddSubview(indicator);
            View.AddSubview(statusLabel);
            View.AddSubview(noticeLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                indicator.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                indicator.CenterYAnchor.ConstraintEqualTo(View.CenterYAnchor, -80),
                statusLabel.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                statusLabel.TopAnchor.ConstraintEqualTo(indicator.BottomAnchor, 40),
                statusLabel.WidthAnchor.ConstraintLessThanOrEqualTo(View.WidthAnchor, 0.8f),
                noticeLabel.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                noticeLabel.TopAnchor.ConstraintEqualTo(statusLabel.BottomAnchor, 30),
                noticeLabel.WidthAnchor.ConstraintLessThanOrEqualTo(View.WidthAnchor, 0.8f)
            ]);
        }

        /// <summary>Aktualisiert die Anzeige aus dem App-Modell.</summary>
        /// <param name="statusText">Statustext des laufenden Start-Workflows.</param>
        /// <param name="notice">Optionaler Ablage-Wiederherstellungshinweis; <c>null</c> = ausgeblendet.</param>
        public void UpdateStatus(string? statusText, string? notice)
        {
            if (statusLabel is not null)
            {
                statusLabel.Text = statusText ?? string.Empty;
            }

            if (noticeLabel is not null)
            {
                noticeLabel.Text = notice ?? string.Empty;
            }
        }
    }
}
