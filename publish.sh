#!/bin/bash

# Navigate to the Host project directory and publish
dotnet publish ./src/Azure.AppConfiguration.Emulator.Host/Azure.AppConfiguration.Emulator.Host.csproj -c Release -o ./publish

echo "Published Host project to ./publish folder"