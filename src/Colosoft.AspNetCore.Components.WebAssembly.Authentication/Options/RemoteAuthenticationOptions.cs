using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticationOptions<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationProviderOptions>
    where TRemoteAuthenticationProviderOptions : new()
{
    public TRemoteAuthenticationProviderOptions ProviderOptions { get; } = new TRemoteAuthenticationProviderOptions();

    public RemoteAuthenticationApplicationPathsOptions AuthenticationPaths { get; } = new RemoteAuthenticationApplicationPathsOptions();

    public RemoteAuthenticationUserOptions UserOptions { get; } = new RemoteAuthenticationUserOptions();
}
