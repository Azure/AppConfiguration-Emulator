#!/usr/bin/env bash

set -euo pipefail

IMAGE="mcr.microsoft.com/azure-app-configuration/app-configuration-emulator:1.0.0-preview"
CONTAINER_NAME="appconfig-emulator"
APP_CONFIG_PORT="${APP_CONFIG_PORT:-8483}"

DATA_DIR="${HOME}/.aace"

ACCESS_KEY_ID="${ACCESS_KEY_ID:-emulator-test-id}"
ACCESS_KEY_SECRET="${ACCESS_KEY_SECRET:-abcdefghijklmnopqrstuvwxyz1234567890}"

export APP_CONFIGURATION_EMULATOR_ENDPOINT="http://localhost:${APP_CONFIG_PORT}"
export APP_CONFIGURATION_EMULATOR_CONNECTION_STRING="Endpoint=${APP_CONFIGURATION_EMULATOR_ENDPOINT};Id=${ACCESS_KEY_ID};Secret=${ACCESS_KEY_SECRET}"

echo "APP_CONFIGURATION_EMULATOR_ENDPOINT=${APP_CONFIGURATION_EMULATOR_ENDPOINT}"
echo "APP_CONFIGURATION_EMULATOR_CONNECTION_STRING=${APP_CONFIGURATION_EMULATOR_CONNECTION_STRING}"

mkdir -p "${DATA_DIR}"
chmod 777 "${DATA_DIR}"

docker pull "${IMAGE}"

docker run -d \
  --name "${CONTAINER_NAME}" \
  -p "${APP_CONFIG_PORT}:8483" \
  -v "${DATA_DIR}:/app/.aace" \
  -e "Tenant:HmacSha256Enabled=true" \
  -e "Tenant:AccessKeys:0:Id=${ACCESS_KEY_ID}" \
  -e "Tenant:AccessKeys:0:Secret=${ACCESS_KEY_SECRET}" \
  "${IMAGE}"
