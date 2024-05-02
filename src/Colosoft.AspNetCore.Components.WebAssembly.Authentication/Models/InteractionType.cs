using System.Text.Json.Serialization;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

[JsonConverter(typeof(JsonStringEnumConverter<InteractionType>))]
public enum InteractionType
{
    SignIn,
    GetToken,
    SignOut,
}
