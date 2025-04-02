using Azure.AppConfiguration.Emulator.ConfigurationSettings;
using Microsoft.AspNetCore.Mvc;

namespace Azure.AppConfiguration.Emulator.Service.Validators
{
    [ModelMetadataType(typeof(KeyValueMetadata))]
    class KeyValueModel : KeyValue
    {
    }
}
