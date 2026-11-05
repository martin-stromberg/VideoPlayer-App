using UIKit;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Fabriken für die UIKit-Standardelemente der Verbindungs- und
    /// Menüseiten — einheitliche Schriftgrößen und Farben im Lean-back-
    /// Erscheinungsbild (große Texte, dunkler Hintergrund). Alle Elemente
    /// sind über die tvOS-Focus-Engine per Siri Remote erreichbar.
    /// </summary>
    internal static class TvosControls
    {
        /// <summary>Seitentitel (groß, fett).</summary>
        /// <param name="text">Der Titeltext.</param>
        /// <returns>Ein konfiguriertes <see cref="UILabel"/>.</returns>
        internal static UILabel Title(string text) => new()
        {
            Text = text,
            TextColor = UIColor.White,
            Font = UIFont.BoldSystemFontOfSize(56),
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        /// <summary>Abschnittsüberschrift innerhalb einer Seite.</summary>
        /// <param name="text">Der Überschriftentext.</param>
        /// <returns>Ein konfiguriertes <see cref="UILabel"/>.</returns>
        internal static UILabel Heading(string text) => new()
        {
            Text = text,
            TextColor = UIColor.White,
            Font = UIFont.BoldSystemFontOfSize(34),
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        /// <summary>Fließtext / Hinweistext (mehrzeilig, gedimmt).</summary>
        /// <param name="text">Der anzuzeigende Text; <c>null</c> = leer.</param>
        /// <returns>Ein konfiguriertes <see cref="UILabel"/>.</returns>
        internal static UILabel Body(string? text = null) => new()
        {
            Text = text ?? string.Empty,
            TextColor = UIColor.LightGray,
            Font = UIFont.SystemFontOfSize(28),
            Lines = 0,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        /// <summary>Inline-Fehlertext (rot, mehrzeilig, initial leer).</summary>
        /// <returns>Ein konfiguriertes <see cref="UILabel"/>.</returns>
        internal static UILabel Error() => new()
        {
            Text = string.Empty,
            TextColor = UIColor.SystemRed,
            Font = UIFont.SystemFontOfSize(28),
            Lines = 0,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        /// <summary>Laufzustands-Text (gedimmt, initial leer).</summary>
        /// <returns>Ein konfiguriertes <see cref="UILabel"/>.</returns>
        internal static UILabel Status() => new()
        {
            Text = string.Empty,
            TextColor = UIColor.LightGray,
            Font = UIFont.SystemFontOfSize(26),
            Lines = 0,
            TranslatesAutoresizingMaskIntoConstraints = false
        };

        /// <summary>
        /// Eingabefeld der Fernbedienungs-Texteingabe — öffnet beim Fokussieren
        /// die tvOS-Systemtastatur (Diktat per Siri Remote bzw.
        /// Continuity-Tastatur des iPhone werden systemseitig unterstützt).
        /// </summary>
        /// <param name="placeholder">Platzhaltertext der leeren Eingabe.</param>
        /// <param name="secure"><c>true</c> = gesicherte Eingabe (Passwort).</param>
        /// <returns>Ein konfiguriertes <see cref="UITextField"/>.</returns>
        internal static UITextField TextField(string placeholder, bool secure = false)
        {
            var field = new UITextField
            {
                Placeholder = placeholder,
                SecureTextEntry = secure,
                BorderStyle = UITextBorderStyle.RoundedRect,
                Font = UIFont.SystemFontOfSize(30),
                TextColor = UIColor.Black,
                AutocorrectionType = UITextAutocorrectionType.No,
                AutocapitalizationType = UITextAutocapitalizationType.None,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            field.HeightAnchor.ConstraintEqualTo(72).Active = true;
            return field;
        }

        /// <summary>Aktions-Button mit Auslöse-Callback (fokussierbar).</summary>
        /// <param name="title">Beschriftung des Buttons.</param>
        /// <param name="action">Auszuführende Aktion bei Auswahl.</param>
        /// <returns>Ein konfigurierter <see cref="UIButton"/>.</returns>
        internal static UIButton Button(string title, Action action)
        {
            var button = UIButton.FromType(UIButtonType.System);
            button.SetTitle(title, UIControlState.Normal);
            button.TranslatesAutoresizingMaskIntoConstraints = false;
            button.PrimaryActionTriggered += (_, _) => action();
            return button;
        }

        /// <summary>
        /// Vertikaler Abschnitts-Stack — fasst Überschrift, Hinweise,
        /// Eingaben und Aktionen einer Kachel zusammen; <c>Hidden</c> blendet
        /// den gesamten Abschnitt samt Layout aus.
        /// </summary>
        /// <param name="children">Die anzuordnenden Views.</param>
        /// <returns>Ein konfigurierter <see cref="UIStackView"/>.</returns>
        internal static UIStackView Section(params UIView[] children) => new(children)
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Alignment = UIStackViewAlignment.Fill,
            Distribution = UIStackViewDistribution.Fill,
            Spacing = 14,
            TranslatesAutoresizingMaskIntoConstraints = false
        };
    }
}
