using Microsoft.Maui.Dispatching;
using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Controls;

/// <summary>
/// Horizontale Medienliste der Startseite (Titel + Poster-Karten) —
/// Muster der alten App, jetzt auf <see cref="HomeMediaItem"/>.
/// Die Auswahl eines Eintrags wird als <see cref="ItemTapped"/>-Ereignis
/// gemeldet; die Seite wandelt sie in eine MVU-Nachricht um.
/// </summary>
public partial class MediaCarousel : ContentView
{
    /// <value>Bindable Property für <see cref="Title"/> — Abschnittsüberschrift der Liste.</value>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(MediaCarousel),
        string.Empty);

    /// <value>Bindable Property für <see cref="Items"/> — einzublendende Einträge; leer = Abschnitt ausgeblendet.</value>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items),
        typeof(IReadOnlyList<HomeMediaItem>),
        typeof(MediaCarousel),
        null,
        propertyChanged: OnItemsChanged);

    /// <value>Bindable Property für <see cref="ItemWidth"/> — Kartenbreite (Standard 150 = Porträt-Poster).</value>
    public static readonly BindableProperty ItemWidthProperty = BindableProperty.Create(
        nameof(ItemWidth),
        typeof(double),
        typeof(MediaCarousel),
        150.0);

    /// <value>Bindable Property für <see cref="ItemHeight"/> — Kartenhöhe (Standard 225 = Porträt-Poster 2:3).</value>
    public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
        nameof(ItemHeight),
        typeof(double),
        typeof(MediaCarousel),
        225.0);

    /// <value>Bindable Property für <see cref="LongPressEnabled"/> — schaltet die 2-s-Long-Press-Erkennung frei (Standard: aus).</value>
    public static readonly BindableProperty LongPressEnabledProperty = BindableProperty.Create(
        nameof(LongPressEnabled),
        typeof(bool),
        typeof(MediaCarousel),
        false);

    /// <summary>Initialisiert eine neue Instanz der <see cref="MediaCarousel"/>-Klasse.</summary>
    public MediaCarousel()
    {
        InitializeComponent();
    }

    /// <summary>Abschnittsüberschrift der Liste.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Einzublendende Einträge; leer = Abschnitt ausgeblendet.</summary>
    public IReadOnlyList<HomeMediaItem>? Items
    {
        get => (IReadOnlyList<HomeMediaItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>
    /// Kartenbreite in Dip. Die Weiterschauen-Liste zeigt querformatige
    /// Vorschaubilder (16:9-Stills) und stellt deshalb breitere Karten;
    /// Poster-Listen belassen den Standard 150.
    /// </summary>
    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>Kartenhöhe in Dip (Standard 225 = Porträt-Poster im Verhältnis 2:3).</summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>
    /// Aktiviert die Long-Press-Erkennung: ein 2-s-Halten eines Eintrags
    /// löst <see cref="ItemLongPressed"/> aus — der kurze Tap bleibt dem
    /// <see cref="ItemTapped"/>-Pfad vorbehalten. Standard: aus, damit
    /// das Kontextmenü auf die jeweils verdrahtete Liste beschränkt
    /// bleibt (Weiterschauen-Liste der Startseite).
    /// </summary>
    public bool LongPressEnabled
    {
        get => (bool)GetValue(LongPressEnabledProperty);
        set => SetValue(LongPressEnabledProperty, value);
    }

    /// <summary>Ein Eintrag wurde per Tippen ausgewählt.</summary>
    public event EventHandler<HomeMediaItem>? ItemTapped;

    /// <summary>Ein Eintrag wurde 2 Sekunden gehalten (Kontextmenü-Anforderung).</summary>
    public event EventHandler<HomeMediaItem>? ItemLongPressed;

    private static void OnItemsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MediaCarousel carousel)
        {
            carousel.IsVisible = (newValue as IReadOnlyList<HomeMediaItem>)?.Count > 0;
        }
    }

    private void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject element && element.BindingContext is HomeMediaItem item)
        {
            ItemTapped?.Invoke(this, item);
        }
    }

    // --- Long-Press-Erkennung (2 s Halten → ItemLongPressed) ---

    /// <summary>Halte-Dauer des Long-Press in Millisekunden (Serveranwendung/Altbestand: 2000 ms).</summary>
    internal const int LongPressDurationMs = 2000;

    private IDispatcherTimer? longPressTimer;
    private HomeMediaItem? pendingLongPressItem;

    /// <summary>
    /// Startet den Long-Press-Timer für den gedrückten Eintrag — nur bei
    /// aktivem <see cref="LongPressEnabled"/>; ein Loslassen oder
    /// Verlassen der Karte vor Ablauf verwirft den Timer, sodass der
    /// kurze Tap unverändert zur Auswahl führt.
    /// </summary>
    /// <param name="sender">Die gedrückte Eintragskarte.</param>
    /// <param name="e">Die Ereignisdaten des Pointer-Drucks.</param>
    private void OnItemPointerPressed(object? sender, PointerEventArgs e)
    {
        if (!LongPressEnabled
            || sender is not BindableObject element
            || element.BindingContext is not HomeMediaItem item)
        {
            return;
        }

        CancelLongPress();
        pendingLongPressItem = item;
        var timer = Dispatcher.CreateTimer();
        longPressTimer = timer;
        timer.Interval = TimeSpan.FromMilliseconds(LongPressDurationMs);
        timer.Tick += OnLongPressElapsed;
        timer.Start();
    }

    private void OnItemPointerEnded(object? sender, PointerEventArgs e)
    {
        CancelLongPress();
    }

    private void OnLongPressElapsed(object? sender, EventArgs e)
    {
        var item = pendingLongPressItem;
        CancelLongPress();
        if (item is not null)
        {
            ItemLongPressed?.Invoke(this, item);
        }
    }

    private void CancelLongPress()
    {
        pendingLongPressItem = null;
        if (longPressTimer is { } timer)
        {
            longPressTimer = null;
            timer.Tick -= OnLongPressElapsed;
            timer.Stop();
        }
    }

    /// <summary>
    /// Löst die Long-Press-Auswahl eines Eintrags aus — identisch zur
    /// Ereigniskette des Timer-Auslaufs. Auslöser ist die Developer-API
    /// (<c>POST /invoke</c> mit <c>gesture: "longpress"</c>): Einträge des
    /// BindableLayout-Karussells fehlen im UIA-Baum und sind sonst nicht
    /// per Automatisierung erreichbar.
    /// </summary>
    /// <param name="itemIndex">Der Index des Eintrags in <see cref="Items"/>.</param>
    internal void SendItemLongPressed(int itemIndex)
    {
        if (Items is { } items && itemIndex >= 0 && itemIndex < items.Count)
        {
            ItemLongPressed?.Invoke(this, items[itemIndex]);
        }
    }
}
