using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticationContext<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>
    where TRemoteAuthenticationState : RemoteAuthenticationState
{
    public string? Url { get; set; }

    public TRemoteAuthenticationState? State { get; set; }

    public InteractiveRequestOptions? InteractiveRequest { get; set; }
}
