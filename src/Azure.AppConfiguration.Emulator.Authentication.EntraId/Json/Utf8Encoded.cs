using Microsoft.AppConfig.Service.Authorization;
using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.Authentication.EntraId
{
    static class Utf8Encoded
    {
        public static readonly JsonEncodedText Subject = Encode("Subject");
        public static readonly JsonEncodedText Attributes = Encode("Attributes");
        public static readonly JsonEncodedText ObjectId = Encode("ObjectId");
        public static readonly JsonEncodedText Groups = Encode("Groups");
        public static readonly JsonEncodedText Actions = Encode("Actions");
        public static readonly JsonEncodedText Id = Encode("Id");
        public static readonly JsonEncodedText IsDataAction = Encode("IsDataAction");
        public static readonly JsonEncodedText Resource = Encode("Resource");
        public static readonly JsonEncodedText ClaimNames = Encode(ClaimTypes.ClaimNames);
        public static readonly JsonEncodedText ClaimSources = Encode(ClaimTypes.ClaimSources);

        private static JsonEncodedText Encode(string value) => JsonEncodedText.Encode(value);
    }
}
