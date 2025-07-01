# Azure App Configuration Emulator

The Azure App Configuration Emulator is a local development tool that provides a lightweight implementation of the Azure App Configuration service. This emulator allows developers to test and develop applications locally without requiring an active Azure subscription or connection to the cloud service.

## Features

- **Local Azure App Configuration API**: Emulates the Azure App Configuration REST API
- **Web UI**: Provides a web-based interface for managing configuration settings
- **Multiple Authentication Methods**: Supports HMAC, Entra ID, and anonymous authentication
- **Configuration Snapshots**: Supports creating and managing configuration snapshots
- **Cross-platform**: Runs on Windows, macOS, and Linux

## Prerequisites

Before building and running the Azure App Configuration Emulator, ensure you have the following technologies installed:

### For Building and Running
- **.NET 8.0 SDK** - Required for building and running the C# application
  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
  - Minimum version: 8.0.100
- **Node.js and npm** - Required for building the React/TypeScript UI component
  - Download from: https://nodejs.org/
  - Minimum Node.js version: 16.x or later

## Build

Follow these steps to build the Azure App Configuration Emulator:

### 1. Clone the Repository
```bash
git clone https://github.com/Azure/AppConfiguration-Emulator.git
cd AppConfiguration-Emulator
```

### 2. Restore Dependencies
Restore all .NET package dependencies:
```bash
dotnet restore
```

### 3. Build the Solution
Build the entire solution including the UI components:
```bash
dotnet build
```

The build process will:
- Compile all C# projects
- Install npm dependencies for the UI component
- Build the React/TypeScript frontend using Vite
- Generate the production-ready UI assets

## Run

To run the Azure App Configuration Emulator locally:

### Start the Emulator
```bash
dotnet run --project src/Azure.AppConfiguration.Emulator.Host/Azure.AppConfiguration.Emulator.Host.csproj
```

### Access the Application
Once started, the emulator will be available at:
- **API Endpoint**: `http://127.0.0.1:8483`
- **Web UI**: `http://127.0.0.1:8483` (serves both API and UI)

### Default Configuration
The emulator runs with the following default settings in development mode:
- **Port**: 8483
- **Authentication**: Anonymous authentication enabled
- **Anonymous User Role**: Owner (full permissions)
- **Logging Level**: Debug

### Stopping the Emulator
Press `Ctrl+C` in the terminal to stop the emulator.

## Test

### Running Tests
Currently, there are no automated tests included in this solution. To run tests (when available):
```bash
dotnet test
```

### Manual Testing
You can manually test the emulator by:

1. **Using the Web UI**: Navigate to `http://127.0.0.1:8483` in your browser
2. **Using Azure SDK**: Configure your Azure App Configuration client to point to `http://127.0.0.1:8483`
3. **Using REST API**: Make HTTP requests directly to the emulator endpoints

### API Testing Example
```bash
# Example: List configuration settings
curl -X GET "http://127.0.0.1:8483/kv" \
  -H "Accept: application/json"
```

## Development

### Project Structure
- `src/Azure.AppConfiguration.Emulator.Host/` - Main application host
- `src/Azure.AppConfiguration.Emulator.Service/` - Core API services
- `src/Azure.AppConfiguration.Emulator.UI/` - Web UI components
- `src/Azure.AppConfiguration.Emulator.Authentication*/` - Authentication providers
- `src/Azure.AppConfiguration.Emulator.ConfigurationSettings/` - Configuration management
- `src/Azure.AppConfiguration.Emulator.ConfigurationSnapshots/` - Snapshot functionality

### Configuration
The emulator can be configured through `appsettings.json` and `appsettings.Development.json` files in the Host project.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
