const connectionString = process.env.APP_CONFIGURATION_EMULATOR_CONNECTION_STRING

if (connectionString === undefined) {
    throw new Error("APP_CONFIGURATION_EMULATOR_CONNECTION_STRING environment variable not found. Please run the start-emulator.ps1 script first.");
}

import { 
    AppConfigurationClient,
    ConfigurationSetting,
    FeatureFlagValue,
    featureFlagContentType,
    featureFlagPrefix
} from "@azure/app-configuration";

const client = new AppConfigurationClient(connectionString, { 
    allowInsecureConnection: true // allow http connections for local emulator
});

const keyValue: ConfigurationSetting<string> = {
    key: "test-key-js-example",
    value: "test-value-js-example",
    isReadOnly: false
};

await client.setConfigurationSetting(keyValue);

const featureFlag: ConfigurationSetting<FeatureFlagValue> = {
    key: `${featureFlagPrefix}test-feature-flag-js-example`,
    contentType: featureFlagContentType,
    value: {
        id: "test-feature-flag-js-example",
        enabled: true,
        conditions: {
            clientFilters: []
        }
    },
    isReadOnly: false
}

await client.setConfigurationSetting(featureFlag);

// For information about Azure App Configuration provider, please go to: https://learn.microsoft.com/azure/azure-app-configuration/configuration-provider-overview
import { load, AzureAppConfiguration } from "@azure/app-configuration-provider";
import {
    ConfigurationMapFeatureFlagProvider,
    FeatureManager 
} from "@microsoft/feature-management";

const settings: AzureAppConfiguration = await load(connectionString, {
    featureFlagOptions: {
        enabled: true
    },
    clientOptions: {
        allowInsecureConnection: true // allow http connections for local emulator
    }
});

const fm = new FeatureManager(new ConfigurationMapFeatureFlagProvider(settings));

console.log(`Configuration setting 'test-key-js-example': ${settings.get("test-key-js-example")}`);
console.log(`Feature flag 'test-feature-flag-js-example': ${await fm.isEnabled("test-feature-flag-js-example")}`);
