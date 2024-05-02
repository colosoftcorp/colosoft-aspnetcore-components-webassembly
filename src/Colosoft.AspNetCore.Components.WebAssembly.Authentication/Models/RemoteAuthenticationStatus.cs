using System.Text.Json.Serialization;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

[JsonConverter(typeof(JsonStringEnumConverter<RemoteAuthenticationStatus>))]
public enum RemoteAuthenticationStatus
{
    Redirect,
    Success,
    Failure,
    OperationCompleted,
}
