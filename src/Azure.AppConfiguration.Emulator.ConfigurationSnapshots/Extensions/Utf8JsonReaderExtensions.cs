using System.Text.Json;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    static class Utf8JsonReaderExtensions
    {
        public static string ReadString(this Utf8JsonReader reader)
        {
            if (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                        return null;

                    case JsonTokenType.String:
                        return reader.GetString();

                    default:
                        break;
                }
            }

            throw new JsonException();
        }

        public static long ReadLong(this Utf8JsonReader reader)
        {
            if (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        return reader.GetInt64();

                    default:
                        break;
                }
            }

            throw new JsonException();
        }

        public static bool ReadBool(this Utf8JsonReader reader)
        {
            if (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.False:
                        return false;

                    case JsonTokenType.True:
                        return true;

                    default:
                        break;
                }
            }

            throw new JsonException();
        }
    }
}
