using System.Text.Json;
using System.Text.Json.Serialization;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false)]
[JsonSerializable(typeof(InteractiveRequestOptions))]
[JsonSerializable(typeof(InteractiveRequestOptions.Converter.OptionsRecord))]
[JsonSerializable(typeof(JsonElement))]
internal partial class InteractiveRequestOptionsSerializerContext : JsonSerializerContext
{
}
