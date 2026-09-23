using Microsoft.Maui.Controls;

namespace VideoWebPlayer.Maui;

/// <summary>
/// Lade-Seite, die während des Startup-Workflows einen Status-Text anzeigt.
/// </summary>
public partial class LoadingPage : ContentPage
{
    /// <summary>
    /// Erstellt eine neue Instanz der <see cref="LoadingPage"/>.
    /// </summary>
    /// <param name="message">Der anzuzeigende Status-Text.</param>
    public LoadingPage(string message)
    {
        InitializeComponent();
        MessageLabel.Text = message;
    }
}
