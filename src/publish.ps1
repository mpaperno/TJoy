# Originally from https://github.com/tlewis17/MSFSTouchPortalPlugin under MIT license.  Thanks Tim!

[CmdletBinding(PositionalBinding=$false)]
Param(
  [string]$Configuration = "Release",
  [string]$Platform = "x64",
  [String]$VersionSuffix = "",
  [switch]$Clean = $false,
  [switch]$BuildAgent = $false
)

$ProjectName = "TouchPortalPlugin"
$DistroName = "TJoy-TouchPortal-Plugin"
$BinarytName = "TJoyTouchPortalPlugin"

$CurrentDir = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Path)

$DistFolderPath = "$CurrentDir\..\packages-dist"
$PluginFilesPath = "$DistFolderPath\$DistroName"

if (Test-Path $PluginFilesPath) {
  Write-Information "Cleaning '$ProjectName' packages-dist folder '$PluginFilesPath'..." -InformationAction Continue
  Remove-Item $PluginFilesPath -Force -Recurse
}

$VersionSuffixCommand = ""
if(-Not ([string]::IsNullOrEmpty($VersionSuffix))) {
  $VersionSuffixCommand = "--version-suffix"
}

if ($Clean) {
  Write-Information "Cleaning '$ProjectName' component....`n" -InformationAction Continue
  dotnet clean "$ProjectName" --configuration $Configuration -p:Platform=$Platform
}

Write-Information "Publishing '$ProjectName' component to '$PluginFilesPath'...`n" -InformationAction Continue
dotnet publish "$ProjectName" --output "$PluginFilesPath\dist" --configuration $Configuration -p:Platform=$Platform $VersionSuffixCommand $VersionSuffix -r "win-$Platform" --self-contained true

# Copy Entry.tp, Readme, Documentation, CHANGELOG to publish
copy "$ProjectName/entry.tp" "$PluginFilesPath"
copy "$ProjectName/vJoyTP.png" "$PluginFilesPath"
copy "..\README.md" "$PluginFilesPath"
copy "..\LICENSE" "$PluginFilesPath\LICENSE.txt"
copy "..\CHANGELOG.md" "$PluginFilesPath"

# Get version
$FileVersion = (Get-Command $PluginFilesPath\dist\$BinarytName.dll).FileVersionInfo.FileVersion

# Create TPP File
$TppFile = "$DistFolderPath\$DistroName-$FileVersion.tpp"
if (Test-Path $TppFile) {
  Remove-Item $TppFile -Force
}
& "C:\Program Files\7-Zip\7z.exe" a "$TppFile" "$DistFolderPath\*" -tzip `-xr!*.tpp

if ($BuildAgent) {
  exit 0
}
