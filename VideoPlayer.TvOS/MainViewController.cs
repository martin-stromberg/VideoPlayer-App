using UIKit;
using VideoPlayer.TvOS.Ui;

namespace VideoPlayer.TvOS
{
    /// <summary>
    /// Fenster-Rahmen der tvOS-App: Container, der den aktuell
    /// modellgetriebenen Inhalt einbettet — die Ladeanzeige des
    /// Start-Workflows, eine Verbindungs-Zustandsseite oder den
    /// Menü-/Inhaltsrahmen (<see cref="MenuTabBarController"/>). Kind-Wechsel
    /// laufen über das Embed-/Remove-Muster von <see cref="UIViewController"/>.
    /// </summary>
    public class MainViewController : UIViewController
    {
        private readonly LoadingViewController loading = new();
        private UIViewController? current;

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            ShowLoading();
        }

        /// <summary>Zeigt die Start-Ladeanzeige.</summary>
        public void ShowLoading() => Show(loading);

        /// <summary>Bettet den angegebenen Inhalt als einziges Kind ein und entfernt den bisherigen.</summary>
        /// <param name="content">Der neue Inhalts-ViewController.</param>
        public void Show(UIViewController content)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (ReferenceEquals(current, content))
            {
                return;
            }

            if (current is not null)
            {
                current.WillMoveToParentViewController(null);
                current.View?.RemoveFromSuperview();
                current.RemoveFromParentViewController();
            }

            current = content;
            AddChildViewController(content);
            content.View!.Frame = View!.Bounds;
            content.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            View.AddSubview(content.View);
            content.DidMoveToParentViewController(this);
        }

        /// <summary>Aktualisiert die Ladeanzeige (Statustext, Ablage-Hinweis).</summary>
        /// <param name="statusText">Statustext des laufenden Start-Workflows.</param>
        /// <param name="notice">Optionaler Ablage-Wiederherstellungshinweis.</param>
        public void UpdateLoadingStatus(string? statusText, string? notice) =>
            loading.UpdateStatus(statusText, notice);
    }
}
