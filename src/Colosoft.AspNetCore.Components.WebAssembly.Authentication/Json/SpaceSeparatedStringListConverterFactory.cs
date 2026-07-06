using System.Text.Json;
using System.Text.Json.Serialization;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication.Json
{
    internal class SpaceSeparatedStringListConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return true;
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return new SpaceSeparatedStringListConverter();
        }

        private sealed class SpaceSeparatedStringListConverter : JsonConverter<List<string>>
        {
            public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var value = reader.GetString();
                return string.IsNullOrEmpty(value)
                    ? new List<string>()
                    : new List<string>(value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }

            public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
            {
                var joined = string.Join(" ", value);
                writer.WriteStringValue(joined);
            }
        }
    }
}
