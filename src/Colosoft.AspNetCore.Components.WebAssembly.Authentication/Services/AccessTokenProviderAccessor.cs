using Microsoft.Extensions.DependencyInjection;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

internal sealed class AccessTokenProviderAccessor : IAccessTokenProviderAccessor
{
    private readonly IServiceProvider provider;
    private IAccessTokenProvider? tokenProvider;

    public AccessTokenProviderAccessor(IServiceProvider provider) => this.provider = provider;

    public IAccessTokenProvider TokenProvider => this.tokenProvider ??= this.provider.GetRequiredService<IAccessTokenProvider>();
}
