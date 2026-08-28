# Testergebnisse

Erstellt am: 2026-08-28

## Bestandene Tests

- `dotnet build VideoWebPlayer.Client/VideoWebPlayer.Client.csproj --no-restore --configuration Release`: Erfolg
- `dotnet build VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-restore --configuration Release`: Erfolg
- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release`: 8/8 bestanden
- `dotnet build VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj --no-restore --configuration Release --framework net10.0-android`: Erfolg (Warnungen)

## Fehlgeschlagene Tests

- `dotnet format --verify-no-changes --no-restore`: Fehlgeschlagen aufgrund bestehender Whitespace- und Formatierungsverstösse im C#-Quellcode.
