# Azure App Configuration Emulator Examples

This directory contains examples demonstrating how to use the Azure App Configuration Emulator for local development.

## Getting Started

### Start the Emulator

Execute the `start-emulator.ps1` script to start the App Configuration emulator:

```powershell
.\start-emulator.ps1
```

The script will:
- Start the emulator in a new PowerShell window
- Set the connection string as a environment variable (`APP_CONFIGURATION_EMULATOR_CONNECTION_STRING`)

When the emulator is successfully started, you'll see the following message in the new PowerShell window:

```
Now listening on: http://127.0.0.1:8483
Application started. Press Ctrl+C to shut down.
```

## .NET SDK Example

Install the following NuGet packages:

```powershell
dotnet add package Azure.Data.AppConfiguration
dotnet add package Microsoft.Extensions.Configuration.AzureAppConfiguration
```

This repository includes a .NET SDK example in the `dotnet-sdk` directory:

```powershell
cd dotnet-sdk/Demo
dotnet build
dotnet run
```

## JavaScript SDK Example

This repository includes a JavaScript SDK example in the `javascript-sdk` directory:

```powershell
cd javascript-sdk

npm install

node azure-sdk.mjs
```