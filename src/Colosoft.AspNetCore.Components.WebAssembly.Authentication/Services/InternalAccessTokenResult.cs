using System.Text.Json.Serialization;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

// Internal for testing purposes
internal readonly struct InternalAccessTokenResult
{
    [JsonConverter(typeof(JsonStringEnumConverter<AccessTokenResultStatus>))]
    public AccessTokenResultStatus Status { get; init; }

    public AccessToken? Token { get; init; }

    public InternalAccessTokenResult(AccessTokenResultStatus status, AccessToken? token)
    {
        this.Status = status;
        this.Token = token;
    }
}
