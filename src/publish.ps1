# Originally from https://github.com/tlewis17/MSFSTouchPortalPlugin under MIT license.  Thanks Tim!

Param(
  [Parameter(Position = 0)]
  [Boolean]$IsBuildAgent = $false,
  [Parameter(Position = 1)]
  [String]$Configuration = "Release",
  [Parameter(Position = 2)]
  [String]$VersionSuffix = ""
)

$ProjectName = "TouchPortalPlugin"
$DistroName = "TJoy-TouchPortal-Plugin"
$BinarytName = "TJoyTouchPortalPlugin"

if((-Not ($IsBuildAgent)) -And ([string]::IsNullOrEmpty($VersionSuffix))) {
  $VersionSuffix = "1"
}

$VersionSuffixCommand = ""
if(-Not ([string]::IsNullOrEmpty($VersionSuffix))) {
  $VersionSuffixCommand = "--version-suffix"
}

Write-Information "Restoring '$ProjectName' component....`n" -InformationAction Continue
dotnet restore "$ProjectName"
#dotnet restore "$ProjectName.Tests"

$CurrentDir = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Path)

$DistFolderPath = "$CurrentDir\..\packages-dist"
$PluginFilesPath = "$DistFolderPath\$DistroName"

if (Test-Path $DistFolderPath) {
  Remove-Item $DistFolderPath -Force -Recurse
}

Write-Information "Cleaning '$ProjectName' packages-dist folder '$DistFolderPath'..." -InformationAction Continue

Write-Information "Publishing '$ProjectName' component to '$PluginFilesPath'...`n" -InformationAction Continue
dotnet publish "$ProjectName" --output "$PluginFilesPath\dist" --configuration $Configuration -p:Platform=x64 $VersionSuffixCommand $VersionSuffix -r "win-x64" --self-contained true

# Copy Entry.tp, Readme, Documentation, CHANGELOG to publish
copy "$ProjectName/entry.tp" "$PluginFilesPath"
copy "$ProjectName/vJoyTP.png" "$PluginFilesPath"
copy "..\README.md" "$PluginFilesPath"
copy "..\LICENSE" "$PluginFilesPath\LICENSE.txt"
copy "..\CHANGELOG.md" "$PluginFilesPath"

# Get version
$FileVersion = (Get-Command $PluginFilesPath\dist\$BinarytName.dll).FileVersionInfo.FileVersion

# Create TPP File
& "C:\Program Files\7-Zip\7z.exe" a $DistFolderPath\$DistroName-$FileVersion.tpp "$DistFolderPath\*" -r -tzip

if ($IsBuildAgent) {
  exit 0
}
