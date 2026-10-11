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

    /// <summary>Ein Eintrag wurde per Tippen ausgewählt.</summary>
    public event EventHandler<HomeMediaItem>? ItemTapped;

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
}
