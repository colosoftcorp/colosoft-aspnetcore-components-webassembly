using Colosoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable S2326 // Unused type parameters should be removed
public interface IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
#pragma warning restore S2326 // Unused type parameters should be removed
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TAccount : RemoteUserAccount
{
    IServiceCollection Services { get; }
}
