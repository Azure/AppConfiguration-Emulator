FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Install Node.js for building the UI
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs

COPY . .

WORKDIR /src

RUN dotnet publish Azure.AppConfiguration.Emulator.Host/Azure.AppConfiguration.Emulator.Host.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-azurelinux3.0-distroless AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8483

ENTRYPOINT ["dotnet", "Azure.AppConfiguration.Emulator.Host.dll"]