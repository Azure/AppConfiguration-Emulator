FROM mcr.microsoft.com/dotnet/aspnet:8.0-azurelinux3.0-distroless

WORKDIR /app

COPY publish/ .

EXPOSE 8483

ENTRYPOINT ["dotnet", "Azure.AppConfiguration.Emulator.Host.dll"]