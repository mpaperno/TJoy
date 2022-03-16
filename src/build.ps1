[CmdletBinding(PositionalBinding=$false)]
Param(
  [string]$ProjectName = "TouchPortalPlugin",
  [string]$Configuration = "Release",
  [string]$Platform = "x64",
  [switch]$Clean = $false,
  [switch]$BuildAgent = $false
)

if ($Clean) {
  Write-Information "Cleaning '$ProjectName' component....`n" -InformationAction Continue
  dotnet clean "$ProjectName" --configuration $Configuration -p:Platform=$Platform
}

Write-Information -MessageData "Building '$ProjectName' component...`n" -InformationAction Continue
dotnet build "$ProjectName" --configuration $Configuration -p:Platform=$Platform

if ($BuildAgent) {
  exit 0
}
