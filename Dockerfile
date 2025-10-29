FROM mcr.microsoft.com/dotnet/aspnet:8.0-azurelinux3.0-distroless

# Create the directory for App Configuration Emulator data so that it can be the volume mount target
WORKDIR /app/.aace

WORKDIR /app

COPY publish/ .

EXPOSE 8483

ENTRYPOINT ["dotnet", "Azure.AppConfiguration.Emulator.Host.dll"]