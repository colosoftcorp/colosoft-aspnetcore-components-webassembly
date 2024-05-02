using System.Text.Json.Serialization;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteUserAccount
{
    [JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties { get; set; } = default!;
}
