using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Tests.Stubs;

/// <summary>
/// Steuerbarer Netzwerkstatus-Monitor für Tests: <see cref="IsOnline"/>
/// und <see cref="IsWifi"/> sind frei setzbar; <see cref="SetOnline"/>
/// meldet den Wechsel zusätzlich über <see cref="INetworkStatusMonitor.AvailabilityChanged"/>.
/// </summary>
internal sealed class FakeNetworkMonitor : INetworkStatusMonitor
{
    public bool IsOnline { get; set; } = true;
    public bool IsWifi { get; set; } = true;
    public event EventHandler<bool>? AvailabilityChanged;
    public void Start() { }
    public void SetOnline(bool online)
    {
        IsOnline = online;
        AvailabilityChanged?.Invoke(this, online);
    }
}
