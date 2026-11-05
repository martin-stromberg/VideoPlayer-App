using UIKit;

namespace VideoPlayer.TvOS
{
    /// <summary>
    /// Leere Startansicht der tvOS-Programmhülle: dunkler Hintergrund mit
    /// zentriertem Titel — bewusst ohne fachliche Funktion.
    /// </summary>
    public class MainViewController : UIViewController
    {
        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            var titleLabel = new UILabel(View.Bounds)
            {
                Text = "Heimkino TV",
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(80),
                TextAlignment = UITextAlignment.Center,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
            };
            View.AddSubview(titleLabel);
        }
    }
}
