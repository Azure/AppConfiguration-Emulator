#!/usr/bin/env pwsh
# start-emulator.ps1
# A script to start the Azure AppConfiguration Emulator with environment variables

$AppConfigPort = "8483"  # Default port used by the emulator
$AccessKeyId = "emulator-test-id"
$AccessKeySecret = "abcdefghijklmnopqrstuvwxyz1234567890"
$connectionString = "Endpoint=http://localhost:$AppConfigPort;Id=$AccessKeyId;Secret=$AccessKeySecret"

# set emulator connection string as a environment variable
$env:APP_CONFIGURATION_EMULATOR_CONNECTION_STRING = $connectionString

$ScriptDir = $PSScriptRoot
$EmulatorProjectPath = Join-Path (Split-Path $ScriptDir -Parent) "src\Azure.AppConfiguration.Emulator.Host\Azure.AppConfiguration.Emulator.Host.csproj"
$EmulatorProjectDir = Split-Path $EmulatorProjectPath -Parent

$emulatorCommand = "cd '$EmulatorProjectDir'; `$env:Tenant__HmacSha256Enabled='true'; `$env:Tenant__AccessKeys__0__Id='$AccessKeyId'; `$env:Tenant__AccessKeys__0__Secret='$AccessKeySecret'; dotnet run"
Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $emulatorCommand

Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host "Azure App Configuration Emulator Started!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host "Connection String:" -ForegroundColor Yellow
Write-Host $connectionString -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Green