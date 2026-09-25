#if ANDROID || IOS
using VideoPlayer.Mvu;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Modale Kamera-Seite des QR-/Ticket-Onboardings (nur iOS/Android):
/// liest den Kopplungs-QR-Code der Serververwaltung
/// (<c>https://server/pairing?t=ticket</c>), dispatcht die
/// <see cref="BootstrapTicketSubmittedMessage"/> und schließt sich —
/// Fortschritt und Fehler zeigt die <see cref="ServerSetupPage"/> über das
/// Model. Windows/MacCatalyst nutzen den manuellen Ticket-Einstieg
/// (ZXing hat keinen Windows-Kamera-Support; die App ist unpackaged).
/// </summary>
public sealed class QrScannerPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private int handled;

    /// <summary>Erstellt die Scanner-Seite.</summary>
    /// <param name="mvuHost">MVU-Host; nimmt die gelesene Kopplungs-Eingabe entgegen.</param>
    public QrScannerPage(MvuHost mvuHost)
    {
        this.mvuHost = mvuHost;
        Title = "QR-Code scannen";

        var cameraView = new CameraBarcodeReaderView
        {
            Options = new BarcodeReaderOptions
            {
                // Der Server stellt QR-Codes aus — andere Formate würden
                // nur Fehltreffer erzeugen.
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            }
        };
        cameraView.BarcodesDetected += OnBarcodesDetected;

        var hint = new Label
        {
            Text = "QR-Code der Serververwaltung scannen",
            HorizontalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(24, 12)
        };

        var cancel = new Button
        {
            Text = "Abbrechen",
            AutomationId = "QrScanCancelButton",
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 16)
        };
        cancel.Clicked += async (_, _) => await CloseAsync();

        Content = new Grid
        {
            RowDefinitions =
            [
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            ],
            Children =
            {
                cameraView,
                hint,
                cancel
            }
        };
        Grid.SetRow(hint, 1);
        Grid.SetRow(cancel, 2);
    }

    /// <summary>
    /// Erste erkannte Kennung übernehmen: die Kopplungs-Eingabe geht an
    /// den MVU-Fluss, danach schließt sich die Seite — ein Flag verhindert
    /// Doppelauslösung bei mehreren Frames.
    /// </summary>
    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var value = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // Atomare Einmal-Übernahme: das Detection-Event kann aus dem
        // Analyse-Loop mehrfach/parallel feuern.
        if (Interlocked.Exchange(ref handled, 1) != 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await mvuHost.DispatchAsync(new BootstrapTicketSubmittedMessage(value));
            await CloseAsync();
        });
    }

    private Task CloseAsync() => Navigation.PopModalAsync();
}
#endif
