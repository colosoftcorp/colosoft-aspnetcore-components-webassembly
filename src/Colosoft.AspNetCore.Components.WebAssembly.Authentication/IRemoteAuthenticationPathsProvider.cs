namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal interface IRemoteAuthenticationPathsProvider
{
    RemoteAuthenticationApplicationPathsOptions ApplicationPaths { get; }
}
