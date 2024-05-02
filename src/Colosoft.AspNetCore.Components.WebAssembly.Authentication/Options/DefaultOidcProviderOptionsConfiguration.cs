using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DefaultOidcOptionsConfiguration : IPostConfigureOptions<RemoteAuthenticationOptions<OidcProviderOptions>>
{
    private readonly NavigationManager navigationManager;

    public DefaultOidcOptionsConfiguration(NavigationManager navigationManager) => this.navigationManager = navigationManager;

    public void Configure(RemoteAuthenticationOptions<OidcProviderOptions> options)
    {
        options.UserOptions.AuthenticationType ??= options.ProviderOptions.ClientId;

        var redirectUri = options.ProviderOptions.RedirectUri;
        if (redirectUri == null || !Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
        {
            redirectUri ??= "authentication/login-callback";
            options.ProviderOptions.RedirectUri = this.navigationManager
                .ToAbsoluteUri(redirectUri).AbsoluteUri;
        }

        var logoutUri = options.ProviderOptions.PostLogoutRedirectUri;
        if (logoutUri == null || !Uri.TryCreate(logoutUri, UriKind.Absolute, out _))
        {
            logoutUri ??= "authentication/logout-callback";
            options.ProviderOptions.PostLogoutRedirectUri = this.navigationManager
                .ToAbsoluteUri(logoutUri).AbsoluteUri;
        }
    }

    public void PostConfigure(string? name, RemoteAuthenticationOptions<OidcProviderOptions> options)
    {
        if (string.Equals(name, Options.DefaultName))
        {
            this.Configure(options);
        }
    }
}
