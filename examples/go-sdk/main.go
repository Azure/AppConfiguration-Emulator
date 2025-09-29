package main

import (
	"context"
	"fmt"
	"log"
	"os"

	"github.com/Azure/AppConfiguration-GoProvider/azureappconfiguration"
	"github.com/Azure/azure-sdk-for-go/sdk/data/azappconfig"
)

type Config struct {
	TestKey string
}

func main() {
	// Get connection string from environment variable
	connectionString := os.Getenv("APP_CONFIGURATION_EMULATOR_CONNECTION_STRING")
	if connectionString == "" {
		log.Fatal("APP_CONFIGURATION_EMULATOR_CONNECTION_STRING environment variable not found. Please run the start-emulator script first.")
	}

	// Create the App Configuration client
	client, err := azappconfig.NewClientFromConnectionString(connectionString, nil)
	if err != nil {
		log.Fatalf("Failed to create client: %v", err)
	}

	ctx := context.Background()

	// Set a configuration setting
	value := "testvalue"

	_, err = client.AddSetting(ctx, "TestKey", &value, nil)
	if err != nil {
		log.Fatalf("Failed to set configuration setting: %v", err)
	}

	// Retrieve by using the AppConfiguration Go provider
	options := &azureappconfiguration.Options{
		Selectors: []azureappconfiguration.Selector{
			{
				KeyFilter: "*",
			},
		},
	}

	authOptions := azureappconfiguration.AuthenticationOptions{
		ConnectionString: connectionString,
	}

	appCfgProvider, err := azureappconfiguration.Load(ctx, authOptions, options)
	if err != nil {
		log.Fatalf("Failed to load configuration: %v", err)
	}

	// Parse configuration into struct
	var config Config
	err = appCfgProvider.Unmarshal(&config, nil)
	if err != nil {
		log.Fatalf("Failed to unmarshal configuration: %v", err)
	}

	fmt.Printf("Retrieved configuration: TestKey = %s\n", config.TestKey)
}
