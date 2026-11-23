using CoreGraphics;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Listenzeile der Downloads-Übersicht: Vorschaubild
    /// (<c>api/pictures/{id}</c>, asynchron über
    /// <see cref="TvosPosterImageService"/> nachgeladen), Titel,
    /// Statuszeile (Medientyp, Zustand, Größe/Fortschritt, Ablaufdatum
    /// bzw. Fehlerhinweis) und Fortschrittsbalken laufender Transfers.
    /// Die Fokussierung hebt die Zeile über die tvOS-Focus-Engine
    /// hervor; die Auswahl öffnet das Aktionsmenü des Eintrags.
    /// </summary>
    public sealed class DownloadTableCell : UITableViewCell
    {
        /// <summary>Wiederverwendungs-Kennung der Zelle für <c>DequeueReusableCell</c>.</summary>
        public const string ReuseId = "DownloadTableCell";

        private static readonly nfloat PreviewWidth = 200f;
        private static readonly nfloat PreviewHeight = 112f;

        private readonly UIImageView previewView;
        private readonly UILabel titleLabel;
        private readonly UILabel statusLabel;
        private readonly UILabel detailLabel;
        private readonly UIProgressView progressView;
        private long? appliedPictureId;

        /// <summary>Initialisiert eine neue Instanz der <see cref="DownloadTableCell"/>-Klasse.</summary>
        public DownloadTableCell()
            : base(UITableViewCellStyle.Default, ReuseId)
        {
            BackgroundColor = UIColor.Clear;

            previewView = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFill,
                ClipsToBounds = true,
                BackgroundColor = UIColor.FromWhiteAlpha(0.14f, 1f),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            previewView.Layer.CornerRadius = 10;

            titleLabel = new UILabel
            {
                TextColor = UIColor.White,
                Font = UIFont.SystemFontOfSize(30),
                Lines = 1,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            statusLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(24),
                Lines = 1,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            detailLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(22),
                Lines = 1,
                LineBreakMode = UILineBreakMode.TailTruncation,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            progressView = new UIProgressView(UIProgressViewStyle.Default)
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            var textStack = new UIStackView([titleLabel, statusLabel, detailLabel])
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Fill,
                Distribution = UIStackViewDistribution.Fill,
                Spacing = 6,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            ContentView.AddSubview(previewView);
            ContentView.AddSubview(textStack);
            ContentView.AddSubview(progressView);

            NSLayoutConstraint.ActivateConstraints(
            [
                previewView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor, 30),
                previewView.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor),
                previewView.WidthAnchor.ConstraintEqualTo(PreviewWidth),
                previewView.HeightAnchor.ConstraintEqualTo(PreviewHeight),

                textStack.LeadingAnchor.ConstraintEqualTo(previewView.TrailingAnchor, 28),
                textStack.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor, -30),
                textStack.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor),

                progressView.LeadingAnchor.ConstraintEqualTo(previewView.LeadingAnchor),
                progressView.TrailingAnchor.ConstraintEqualTo(previewView.TrailingAnchor),
                progressView.BottomAnchor.ConstraintEqualTo(previewView.BottomAnchor),
                progressView.HeightAnchor.ConstraintEqualTo(6)
            ]);
        }

        /// <inheritdoc/>
        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            appliedPictureId = null;
            previewView.Image = null;
            titleLabel.Text = string.Empty;
            statusLabel.Text = string.Empty;
            detailLabel.Text = string.Empty;
            progressView.Hidden = true;
            progressView.Progress = 0f;
        }

        /// <summary>Belegt die Zeile mit einem Download-Eintrag und stößt ggf. den asynchronen Bild-Abruf an.</summary>
        /// <param name="item">Der anzuzeigende Download-Eintrag.</param>
        /// <param name="posters">Der Bild-Cache-/Ladedienst (<c>api/pictures/{id}</c>).</param>
        public void Update(DownloadListItem item, TvosPosterImageService posters)
        {
            titleLabel.Text = item.Title;

            var status = $"{item.MediaTypeText} · {item.StateText}";
            if (item.HasSizeText)
            {
                status += $" · {item.SizeText}";
            }

            if (item.State == DownloadState.Downloading)
            {
                status += $" · {item.ProgressPercentText}";
            }

            statusLabel.Text = status;
            detailLabel.Text = item.ErrorText ?? item.ExpiresText ?? string.Empty;
            detailLabel.TextColor = item.HasError
                ? UIColor.SystemRed
                : UIColor.LightGray;

            progressView.Hidden = !item.ShowProgress;
            progressView.Progress = (float)item.Progress;

            appliedPictureId = item.PictureId;
            previewView.Image = null;
            if (item.PictureId is > 0)
            {
                _ = LoadPreviewAsync(item.PictureId.Value, posters);
            }
        }

        private async Task LoadPreviewAsync(long pictureId, TvosPosterImageService posters)
        {
            var image = await posters.GetImageAsync(pictureId);
            if (image is null || appliedPictureId != pictureId)
            {
                // Verworfenes Ergebnis einer bereits recycelten Zelle
                // oder kein Bild verfügbar — der getönte Rahmen bleibt.
                return;
            }

            previewView.Image = image;
        }
    }
}
