const connectionString = process.env.APP_CONFIGURATION_EMULATOR_CONNECTION_STRING

if (connectionString === undefined) {
    throw new Error("APP_CONFIGURATION_EMULATOR_CONNECTION_STRING environment variable not found. Please run the start-emulator script first.");
}

import { AppConfigurationClient } from "@azure/app-configuration";

const client = new AppConfigurationClient(connectionString, { 
    allowInsecureConnection: true // allow http connections for local emulator
});

await client.setConfigurationSetting({
  key: "testkey",
  value: "testvalue"
});

// For information about Azure App Configuration provider, please go to: https://learn.microsoft.com/azure/azure-app-configuration/configuration-provider-overview
import {load } from "@azure/app-configuration-provider";

const settings = await load(connectionString, {
    clientOptions: {
        allowInsecureConnection: true // allow http connections for local emulator
    }
});

console.log(`Configuration setting for 'testkey': ${settings.get("testkey")}`);

