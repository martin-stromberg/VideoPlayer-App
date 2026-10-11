using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// FlaUI-Hilfen für die echte UIA-Eingabekette: Elemente per
/// AutomationId finden (mit Retry — die MAUI-Oberfläche baut sich
/// asynchron auf), Text per Value-Pattern setzen und Buttons per
/// Invoke-Pattern auslösen (kein Koordinaten-/Mausklick).
/// </summary>
public sealed class UiDriver : IDisposable
{
    private readonly UIA3Automation automation;
    private readonly Window window;

    /// <summary>Erstellt den Treiber für das Hauptfenster der App.</summary>
    /// <param name="automation">Geteilte UIA3-Automation der App-Instanz.</param>
    /// <param name="window">Das Hauptfenster.</param>
    public UiDriver(UIA3Automation automation, Window window)
    {
        this.automation = automation;
        this.window = window;
    }

    /// <summary>ConditionFactory der UIA3-Automation.</summary>
    public ConditionFactory Conditions => automation.ConditionFactory;

    /// <summary>
    /// Findet ein Element per AutomationId im Fenster (Retry bis Timeout).
    /// </summary>
    /// <param name="automationId">Die gesuchte AutomationId.</param>
    /// <param name="timeout">Maximale Wartezeit (Standard 15 s).</param>
    /// <returns>Das gefundene Element.</returns>
    public AutomationElement FindByAutomationId(string automationId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var element = window.FindFirstDescendant(
                Conditions.ByAutomationId(automationId));
            if (element is not null)
            {
                return element;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Kein Element mit der AutomationId '{automationId}' im Fenster gefunden.");
            }

            Thread.Sleep(200);
        }
    }

    /// <summary>Prüft, ob ein Element mit der AutomationId sichtbar existiert.</summary>
    /// <param name="automationId">Die gesuchte AutomationId.</param>
    /// <returns><c>true</c>, wenn das Element existiert und nicht offscreen liegt.</returns>
    public bool Exists(string automationId)
    {
        try
        {
            var element = window.FindFirstDescendant(Conditions.ByAutomationId(automationId));
            return element is not null && !element.Properties.IsOffscreen.ValueOrDefault;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Setzt den Text eines Eingabefelds über das UIA-Value-Pattern
    /// (echte UI-Eingabekette — der MAUI-Handler feuert
    /// <c>TextChanged</c> wie eine Tastatureingabe). Danach wird der
    /// Feldwert zurückgelesen; ist er nicht gelandet (Seite noch im
    /// Aufbau / Fokus verloren), wird die Eingabe bis zum
    /// <paramref name="timeout"/> wiederholt.
    /// </summary>
    /// <param name="automationId">AutomationId des Eingabefelds.</param>
    /// <param name="text">Der einzugebende Text.</param>
    /// <param name="timeout">Maximale Wartezeit (Standard 15 s).</param>
    public void EnterText(string automationId, string text, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < deadline)
        {
            var element = window.FindFirstDescendant(Conditions.ByAutomationId(automationId));
            if (element is not null)
            {
                try
                {
                    var textBox = element.AsTextBox();
                    textBox.Focus();
                    if (element.Properties.IsPassword.ValueOrDefault)
                    {
                        // Passwortboxen blenden den Wert aus (ValuePattern
                        // liefert nie den Klartext) — dort bleibt reine
                        // Tastatureingabe ohne Rücklesen.
                        textBox.Enter(text);
                        return;
                    }

                    // ValuePattern setzt den Text atomar — simulierte Tasten
                    // gehen unter Last gelegentlich verloren (Truncated-Input-
                    // Flakes wie „server" statt „ftp://server"). Feuert
                    // TextChanged ebenso.
                    if (textBox.Patterns.Value.IsSupported)
                    {
                        textBox.Patterns.Value.Pattern.SetValue(text);
                        Thread.Sleep(150);
                        var current = textBox.Patterns.Value.Pattern.Value.ValueOrDefault;
                        if (string.Equals(current, text, StringComparison.Ordinal))
                        {
                            return;
                        }
                    }
                    else
                    {
                        textBox.Enter(text);
                        return;
                    }
                }
                catch (Exception)
                {
                    // Element-Referenz veraltet oder noch nicht bereit —
                    // erneut suchen bis zum Timeout.
                }
            }

            Thread.Sleep(200);
        }

        throw new TimeoutException(
            $"Element mit AutomationId '{automationId}' nahm den Text nicht auf.");
    }

    /// <summary>
    /// Löst einen Button per UIA-Invoke-Pattern aus (echtes Klick-
    /// Äquivalent ohne Maus-/Koordinatenklick).
    /// </summary>
    /// <param name="automationId">AutomationId des Buttons.</param>
    public void ClickButton(string automationId)
    {
        var element = FindByAutomationId(automationId);
        var button = element.AsButton();
        button.Invoke();
    }

    /// <summary>
    /// Schaltet einen Schalter (<c>Switch</c>) per UIA-TogglePattern um;
    /// ist das Muster nicht verfügbar, greifen Invoke bzw. Klick als
    /// Fallback.
    /// </summary>
    /// <param name="automationId">AutomationId des Schalters.</param>
    public void Toggle(string automationId)
    {
        var element = FindByAutomationId(automationId);
        if (element.Patterns.Toggle.IsSupported)
        {
            element.Patterns.Toggle.Pattern.Toggle();
        }
        else if (element.Patterns.Invoke.IsSupported)
        {
            element.Patterns.Invoke.Pattern.Invoke();
        }
        else
        {
            element.Click();
        }
    }

    /// <summary>
    /// Öffnet den angegebenen Menüeintrag des Shell-Flyouts per Name
    /// (z. B. „Einstellungen"). Öffnet bei Bedarf zuerst das Flyout
    /// über die NavigationView-Umschalttaste. Akzeptiert nur Elemente,
    /// die wie ein Menüeintrag aussehen (ListItem/MenuItem) — sonst
    /// würden Titeltexte anderer Seiten mitklicken.
    /// </summary>
    /// <param name="title">Anzeigename des Menüeintrags.</param>
    /// <param name="timeout">Maximale Wartezeit je Versuch.</param>
    public void OpenFlyoutItem(string title, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        var flyoutOpened = false;
        while (DateTime.UtcNow < deadline)
        {
            var item = FindMenuItem(title);
            if (item is not null)
            {
                // NavigationView-Einträge sind Listen-Elemente:
                // SelectionItem ist ihr natives Muster; Invoke und der
                // Mausklick sind Fallbacks.
                if (item.Patterns.SelectionItem.IsSupported)
                {
                    item.Patterns.SelectionItem.Pattern.Select();
                }
                else if (item.Patterns.Invoke.IsSupported)
                {
                    item.Patterns.Invoke.Pattern.Invoke();
                }
                else
                {
                    item.Click();
                }

                return;
            }

            if (!flyoutOpened)
            {
                var toggle = FindFlyoutToggle();
                if (toggle is not null)
                {
                    if (toggle.Patterns.Invoke.IsSupported)
                    {
                        toggle.Patterns.Invoke.Pattern.Invoke();
                    }
                    else
                    {
                        toggle.Click();
                    }

                    flyoutOpened = true;
                }
            }

            Thread.Sleep(300);
        }

        throw new TimeoutException($"Der Menüeintrag '{title}' wurde im Fenster nicht gefunden.");
    }

    /// <summary>
    /// Sucht den Flyout-Menüeintrag (<c>ListItem</c> mit dem Titel in der
    /// Navigationsliste) — Titeltexte der aktuellen Seite werden ignoriert.
    /// </summary>
    /// <param name="title">Anzeigename des Menüeintrags.</param>
    /// <returns>Das Element oder <c>null</c>.</returns>
    private AutomationElement? FindMenuItem(string title)
    {
        return window
            .FindAllDescendants(Conditions.ByName(title))
            .FirstOrDefault(e =>
                e.ControlType is ControlType.ListItem or ControlType.MenuItem);
    }

    /// <summary>
    /// Sucht die Flyout-Umschalttaste der Shell. MAUI vergibt auf Windows
    /// die AutomationId <c>OK</c> und einen lokalisierten Namen
    /// („Navigation öffnen"/„Open Navigation") — beides wird akzeptiert.
    /// </summary>
    /// <returns>Der Button oder <c>null</c>.</returns>
    private AutomationElement? FindFlyoutToggle()
    {
        var buttons = window
            .FindAllDescendants(Conditions.ByControlType(ControlType.Button));
        return buttons.FirstOrDefault(b =>
                b.Name?.StartsWith("Navigation", StringComparison.OrdinalIgnoreCase) == true
                || string.Equals(b.Name, "Open Navigation", StringComparison.OrdinalIgnoreCase))
            ?? buttons.FirstOrDefault(b =>
                string.Equals(b.AutomationId, "OK", StringComparison.Ordinal));
    }

    /// <summary>
    /// Wartet, bis ein Element mit der AutomationId sichtbar im Fenster
    /// existiert; rollt es dabei über <c>ScrollItemPattern</c> in den
    /// sichtbaren Bereich (Elemente in einer ScrollView meldet UIA erst
    /// als sichtbar, wenn sie eingerollt sind). Liefert <c>false</c>,
    /// wenn das Element bis zum Timeout nicht sichtbar wird — damit
    /// taugt es auch für Negativ-Behauptungen.
    /// </summary>
    /// <param name="automationId">Die gesuchte AutomationId.</param>
    /// <param name="timeout">Maximale Wartezeit (Standard 15 s).</param>
    /// <returns><c>true</c>, wenn das Element sichtbar wurde.</returns>
    public bool WaitForVisible(string automationId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (DateTime.UtcNow < deadline)
        {
            var element = window.FindFirstDescendant(Conditions.ByAutomationId(automationId));
            if (element is not null)
            {
                if (element.Patterns.ScrollItem.IsSupported)
                {
                    try
                    {
                        element.Patterns.ScrollItem.Pattern.ScrollIntoView();
                    }
                    catch (Exception)
                    {
                        // ScrollIntoView kann auf nicht scrollbar gehosteten
                        // Elementen fehlschlagen — Sichtbarkeit dennoch prüfen.
                    }
                }

                if (!element.Properties.IsOffscreen.ValueOrDefault)
                {
                    return true;
                }
            }

            Thread.Sleep(200);
        }

        return false;
    }

    /// <summary>
    /// Wählt einen Eintrag einer ComboBox (MAUI-<c>Picker</c> meldet sich
    /// auf Windows als ComboBox) per Anzeigetext aus: aufklappen,
    /// Listeneintrag per Name suchen und per SelectionItem/Invoke wählen.
    /// </summary>
    /// <param name="automationId">AutomationId der ComboBox.</param>
    /// <param name="itemText">Anzeigetext des zu wählenden Eintrags.</param>
    public void SelectComboBoxItem(string automationId, string itemText)
    {
        var combo = FindByAutomationId(automationId).AsComboBox();
        combo.Expand();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (true)
        {
            var item = combo.Items.FirstOrDefault(i =>
                string.Equals(i.Name, itemText, StringComparison.Ordinal));
            if (item is not null)
            {
                if (item.Patterns.SelectionItem.IsSupported)
                {
                    item.Patterns.SelectionItem.Pattern.Select();
                }
                else if (item.Patterns.Invoke.IsSupported)
                {
                    item.Patterns.Invoke.Pattern.Invoke();
                }
                else
                {
                    item.Click();
                }

                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Kein ComboBox-Eintrag '{itemText}' unter '{automationId}' gefunden "
                    + $"({string.Join(", ", combo.Items.Select(i => i.Name))}).");
            }

            Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Liest die Anzeigenamen der Einträge einer ComboBox in
    /// Reihenfolge (klappt die Liste dafür auf und wieder zu).
    /// </summary>
    /// <param name="automationId">AutomationId der ComboBox.</param>
    /// <returns>Die Eintragsnamen in Anzeigereihenfolge.</returns>
    public IReadOnlyList<string> GetComboBoxItemNames(string automationId)
    {
        var combo = FindByAutomationId(automationId).AsComboBox();
        combo.Expand();
        try
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
            while (combo.Items.Length == 0 && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(200);
            }

            return combo.Items.Select(i => i.Name ?? string.Empty).ToList();
        }
        finally
        {
            combo.Collapse();
        }
    }

    /// <summary>Sucht ein Element per Anzeigename (einmalig, ohne Retry).</summary>
    /// <param name="name">Der gesuchte Name.</param>
    /// <returns>Das Element oder <c>null</c>.</returns>
    public AutomationElement? FindByName(string name)
    {
        return window.FindFirstDescendant(Conditions.ByName(name));
    }

    /// <summary>
    /// Löst ein Element per Anzeigename aus — für Dialog-Schaltflächen
    /// (z. B. <c>DisplayActionSheet</c>), die als Popup außerhalb des
    /// Fenster-Teilbaums liegen können: zuerst im Fenster, danach auf
    /// Desktop-Ebene eingeschränkt auf den App-Prozess (Retry bis
    /// Timeout).
    /// </summary>
    /// <param name="name">Der Anzeigename des Elements.</param>
    /// <param name="timeout">Maximale Wartezeit (Standard 15 s).</param>
    /// <returns>Das gefundene und ausgelöste Element.</returns>
    public AutomationElement ClickByName(string name, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        var processId = window.Properties.ProcessId.ValueOrDefault;
        while (true)
        {
            var element = window.FindFirstDescendant(Conditions.ByName(name))
                ?? automation.GetDesktop()
                    .FindAllDescendants(Conditions.ByName(name))
                    .FirstOrDefault(e => e.Properties.ProcessId.ValueOrDefault == processId);
            if (element is not null)
            {
                if (element.Patterns.Invoke.IsSupported)
                {
                    element.Patterns.Invoke.Pattern.Invoke();
                }
                else
                {
                    element.Click();
                }

                return element;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"Kein Element mit dem Namen '{name}' im Fenster oder als Popup des Prozesses gefunden.");
            }

            Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Schreibt den UIA-Baum des Fensters (Name, AutomationId,
    /// ControlType je Knoten) in eine Textdatei — Diagnosehilfe,
    /// wenn ein Element nicht gefunden wird.
    /// </summary>
    /// <param name="path">Zieldatei.</param>
    public void DumpTree(string path)
    {
        var lines = new List<string>();
        DumpElement(window, 0, lines);
        File.WriteAllLines(path, lines);
    }

    private static void DumpElement(AutomationElement element, int depth, List<string> lines)
    {
        string name, automationId, controlType;
        try { name = element.Name ?? string.Empty; } catch { name = "?"; }
        try { automationId = element.AutomationId ?? string.Empty; } catch { automationId = "?"; }
        try { controlType = element.ControlType.ToString(); } catch { controlType = "?"; }
        lines.Add($"{new string(' ', depth * 2)}{controlType} | id={automationId} | name={name}");
        try
        {
            foreach (var child in element.FindAllChildren())
            {
                DumpElement(child, depth + 1, lines);
            }
        }
        catch { /* Baum kann sich während des Dumps ändern */ }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Automation und Prozess gehören der AppInstance — hier nichts
        // zu räumen (Dispose nur für symmetrische Nutzung im Test).
    }
}
