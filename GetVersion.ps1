$jsonContent = Get-Content -Path "App\appsettings.json" -Raw | ConvertFrom-Json
$version = $jsonContent.AppSettings.Version
Write-Output $version 