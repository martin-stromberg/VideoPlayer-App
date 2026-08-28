#Requires -Version 7.2
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$tag = $Version -replace '^v', ''

if ($tag -notmatch '^([0-9]+)\.([0-9]+)\.([0-9]+)') {
    throw "Version '$Version' does not match a semantic version tag."
}

$major = [int]$matches[1]
$minor = [int]$matches[2]
$patch = [int]$matches[3]

$applicationDisplayVersion = "$major.$minor.$patch"
$applicationVersion = $major * 10000 + $minor * 100 + $patch

$csprojPath = 'VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj'
[xml]$project = Get-Content $csprojPath

$propertyGroup = $project.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion } | Select-Object -First 1
if (-not $propertyGroup) {
    throw "Could not find a PropertyGroup with ApplicationDisplayVersion in $csprojPath"
}

$propertyGroup.ApplicationDisplayVersion = $applicationDisplayVersion
$propertyGroup.ApplicationVersion = [string]$applicationVersion
$project.Save($csprojPath)
