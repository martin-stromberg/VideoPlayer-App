$client = New-Object System.Net.Sockets.UdpClient
$client.EnableBroadcast = $true
$client.Client.ReceiveTimeout = 3000
$bytes = [System.Text.Encoding]::UTF8.GetBytes('VIDEOWEBPLAYER_DISCOVERY')
$broadcast = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Broadcast, 5001)
[void]$client.Send($bytes, $bytes.Length, $broadcast)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$found = @()
while ($sw.ElapsedMilliseconds -lt 3000) {
    try {
        if ($client.Available -gt 0) {
            $remote = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
            $resp = $client.Receive([ref]$remote)
            $text = [System.Text.Encoding]::UTF8.GetString($resp)
            $found += "$($remote.Address):$($remote.Port) -> $text"
        }
    } catch {}
    Start-Sleep -Milliseconds 100
}
if ($found.Count -eq 0) { Write-Output 'KEINE ANTWORT auf UDP-Broadcast Port 5001' } else { $found | ForEach-Object { Write-Output $_ } }
$client.Close()
