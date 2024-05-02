using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DefaultRemoteApplicationPathsProvider<[DynamicallyAccessedMembers(JsonSerialized)] TProviderOptions> : IRemoteAuthenticationPathsProvider
    where TProviderOptions : class, new()
{
    private readonly IOptions<RemoteAuthenticationOptions<TProviderOptions>> options;

    public DefaultRemoteApplicationPathsProvider(IOptionsSnapshot<RemoteAuthenticationOptions<TProviderOptions>> options)
    {
        this.options = options;
    }

    public RemoteAuthenticationApplicationPathsOptions ApplicationPaths => this.options.Value.AuthenticationPaths;
}
