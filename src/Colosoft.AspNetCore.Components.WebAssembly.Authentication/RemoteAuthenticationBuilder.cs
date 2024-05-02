using Microsoft.Extensions.DependencyInjection;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
    : IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TAccount : RemoteUserAccount
{
    public RemoteAuthenticationBuilder(IServiceCollection services) => this.Services = services;

    public IServiceCollection Services { get; }
}
