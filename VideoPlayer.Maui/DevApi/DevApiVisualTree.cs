#if DEBUG || DEV_API
using System.Collections;
using System.Reflection;

namespace VideoPlayer.Maui.DevApi;

/// <summary>
/// Baut den logischen Elementbaum für <c>GET /tree</c> auf und führt die
/// Element-Operationen der Developer-API aus (AutomationId-Lookup,
/// Clicked-Äquivalent, Textsetzen). Alle Methoden müssen auf dem
/// UI-Thread laufen.
/// </summary>
public static class DevApiVisualTree
{
    private static readonly string[] TextPropertyNames = ["NameOrAddress", "Name", "Title", "DisplayName", "Text"];

    /// <summary>Baut den Baum rekursiv ab dem übergebenen Element auf.</summary>
    /// <param name="root">Wurzelelement (üblicherweise die aktuelle Page).</param>
    /// <returns>Der serialisierbare Baum.</returns>
    public static DevApiTreeNode BuildTree(Element root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var children = new List<DevApiTreeNode>();
        foreach (var child in VisualChildren(root))
        {
            children.Add(BuildTree(child));
        }

        return new DevApiTreeNode
        {
            AutomationId = root.AutomationId,
            Type = root.GetType().Name,
            IsVisible = (root as VisualElement)?.IsVisible ?? true,
            Bounds = BoundsOf(root),
            Text = TextOf(root),
            Placeholder = (root as InputView)?.Placeholder,
            Children = children,
            Items = ItemsOf(root)
        };
    }

    /// <summary>Sucht ein Element anhand seiner AutomationId im Teilbaum.</summary>
    /// <param name="root">Wurzelelement der Suche.</param>
    /// <param name="automationId">Die gesuchte AutomationId.</param>
    /// <returns>Das Element oder <c>null</c>.</returns>
    public static Element? FindByAutomationId(Element root, string automationId)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (string.Equals(root.AutomationId, automationId, StringComparison.Ordinal))
        {
            return root;
        }

        foreach (var child in VisualChildren(root))
        {
            var found = FindByAutomationId(child, automationId);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Löst das Clicked-Äquivalent eines Elements aus: <c>SendClicked()</c>
    /// auf Buttons, Toggle auf Schaltern, <c>Activate()</c>/Command auf
    /// Menüeinträgen, Command-Quellen und Tap-Gesten mit Command.
    /// </summary>
    /// <param name="element">Das auszulösende Element.</param>
    /// <param name="error">Deutscher Fehlertext, wenn das Element nicht auslösbar ist.</param>
    /// <returns><c>true</c>, wenn eine Aktion ausgelöst wurde.</returns>
    public static bool TryInvoke(Element element, out string? error)
    {
        ArgumentNullException.ThrowIfNull(element);
        error = null;
        switch (element)
        {
            case Button button:
                button.SendClicked();
                return true;
            case ImageButton imageButton:
                imageButton.SendClicked();
                return true;
            case Switch toggle:
                toggle.IsToggled = !toggle.IsToggled;
                return true;
            case CheckBox checkBox:
                checkBox.IsChecked = !checkBox.IsChecked;
                return true;
            case MenuItem menuItem:
                ((IMenuItemController)menuItem).Activate();
                return true;
        }

        // Generischer Fallback: Steuerelemente mit Command-Property
        // (z. B. ToolbarItem, eigene Controls) sowie Tap-Gesten mit Command.
        if (TryExecuteCommand(element))
        {
            return true;
        }

        if (element is View view)
        {
            foreach (var recognizer in view.GestureRecognizers)
            {
                if (recognizer is TapGestureRecognizer tap && TryExecuteCommand(tap))
                {
                    return true;
                }
            }
        }

        error = $"Das Element vom Typ '{element.GetType().Name}' ist nicht auslösbar.";
        return false;
    }

    /// <summary>
    /// Führt <c>Command</c>/<c>CommandParameter</c> eines Objekts per
    /// Reflection aus (kein gemeinsames öffentliches Interface in MAUI).
    /// </summary>
    private static bool TryExecuteCommand(object source)
    {
        var type = source.GetType();
        var command = type.GetProperty("Command", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(source) as System.Windows.Input.ICommand;
        if (command is null)
        {
            return false;
        }

        var parameter = type.GetProperty("CommandParameter", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(source);
        if (!command.CanExecute(parameter))
        {
            return false;
        }

        command.Execute(parameter);
        return true;
    }

    /// <summary>
    /// Wählt den Eintrag <paramref name="itemIndex"/> einer <see cref="CollectionView"/>
    /// aus (<c>SelectedItem</c>-Setter — feuert <c>SelectionChanged</c> wie ein
    /// Fingertipp). Schließt die UIA-Lücke: CollectionView-Einträge fehlen unter
    /// Windows im Automation-Baum und sind sonst nur per Koordinatenklick
    /// erreichbar.
    /// </summary>
    /// <param name="element">Das Zielelement (muss eine <see cref="CollectionView"/> sein).</param>
    /// <param name="itemIndex">Nullbasierter Index in der <c>ItemsSource</c>.</param>
    /// <param name="error">Deutscher Fehlertext, wenn die Auswahl nicht möglich ist.</param>
    /// <returns><c>true</c>, wenn der Eintrag ausgewählt wurde.</returns>
    public static bool TrySelectItem(Element element, int itemIndex, out string? error)
    {
        ArgumentNullException.ThrowIfNull(element);
        error = null;
        if (element is not CollectionView collectionView)
        {
            error = $"Das Element vom Typ '{element.GetType().Name}' ist keine CollectionView.";
            return false;
        }

        if (collectionView.ItemsSource is not IEnumerable items)
        {
            error = "Die CollectionView hat keine ItemsSource.";
            return false;
        }

        object? target = null;
        var index = 0;
        var count = 0;
        foreach (var item in items)
        {
            if (index++ == itemIndex)
            {
                target = item;
            }

            count++;
        }

        if (itemIndex < 0 || itemIndex >= count)
        {
            error = $"Der Item-Index {itemIndex} liegt außerhalb der Liste ({count} Einträge).";
            return false;
        }

        collectionView.SelectedItem = target;
        return true;
    }

    /// <summary>
    /// Setzt den Text eines Eingabefelds (<see cref="InputView"/>); der
    /// Setter feuert das <c>TextChanged</c>-Ereignis wie eine Eingabe.
    /// </summary>
    /// <param name="element">Das Zielelement.</param>
    /// <param name="text">Der zu setzende Text.</param>
    /// <param name="error">Deutscher Fehlertext, wenn das Element kein Eingabefeld ist.</param>
    /// <returns><c>true</c>, wenn der Text gesetzt wurde.</returns>
    public static bool TrySetText(Element element, string text, out string? error)
    {
        ArgumentNullException.ThrowIfNull(element);
        error = null;
        if (element is InputView input)
        {
            input.Text = text;
            return true;
        }

        error = $"Das Element vom Typ '{element.GetType().Name}' ist kein Texteingabefeld.";
        return false;
    }

    /// <summary>
    /// Liefert die visuellen Kinder; bei der Shell wird zusätzlich die
    /// aktuell angezeigte Seite eingehängt, da sie nicht zwingend als
    /// visuelles Kind der Shell auftaucht.
    /// </summary>
    private static IEnumerable<Element> VisualChildren(Element element)
    {
        var children = new List<Element>();
        if (element is IVisualTreeElement treeElement)
        {
            foreach (var child in treeElement.GetVisualChildren())
            {
                if (child is Element childElement)
                {
                    children.Add(childElement);
                }
            }
        }

        if (element is Shell shell
            && shell.CurrentPage is { } currentPage
            && !children.Contains(currentPage))
        {
            children.Add(currentPage);
        }

        return children;
    }

    private static DevApiBounds? BoundsOf(Element element)
    {
        if (element is not VisualElement visual)
        {
            return null;
        }

        var bounds = visual.Bounds;
        return new DevApiBounds { X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height };
    }

    private static string? TextOf(Element element) => element switch
    {
        Label label => label.Text,
        Button button => button.Text,
        InputView input => input.Text,
        MenuItem menuItem => menuItem.Text,
        _ => null
    };

    /// <summary>
    /// Leitet die Einträge einer <c>CollectionView</c> aus deren
    /// <c>ItemsSource</c> ab (UIA-Lücke: die Items erscheinen unter Windows
    /// nicht im Automation-Baum).
    /// </summary>
    private static IReadOnlyList<DevApiTreeItem>? ItemsOf(Element element)
    {
        if (element is not CollectionView collectionView || collectionView.ItemsSource is not IEnumerable items)
        {
            return null;
        }

        var result = new List<DevApiTreeItem>();
        var index = 0;
        foreach (var item in items)
        {
            result.Add(new DevApiTreeItem
            {
                Index = index++,
                Type = item?.GetType().Name ?? string.Empty,
                Text = ItemText(item),
                Detail = ItemProperty(item, "Address")
            });
        }

        return result;
    }

    /// <summary>Anzeigetext eines Listeneintrags über die bekannten Anzeige-Properties.</summary>
    private static string? ItemText(object? item)
    {
        if (item is null)
        {
            return null;
        }

        foreach (var name in TextPropertyNames)
        {
            var value = ItemProperty(item, name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return item.ToString();
    }

    /// <summary>Liest eine benannte String-Property per Reflection (für Item-Modelle ohne gemeinsame Schnittstelle).</summary>
    private static string? ItemProperty(object? item, string propertyName)
    {
        var property = item?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.PropertyType == typeof(string)
            ? property.GetValue(item) as string
            : null;
    }
}
#endif
