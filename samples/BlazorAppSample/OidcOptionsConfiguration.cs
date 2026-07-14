using Colosoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace BlazorAppSample
{
    public class OidcOptionsConfiguration(
        NavigationManager navigationManager)
        : IPostConfigureOptions<RemoteAuthenticationOptions<OidcProviderOptions>>
    {
        public void PostConfigure(string? name, RemoteAuthenticationOptions<OidcProviderOptions> options)
        {
            options.ProviderOptions.ResponseType = "code";
            options.ProviderOptions.Scope.Add("offline_access");
            options.ProviderOptions.Scope.Add("profile");

            var redirectUri = options.ProviderOptions.RedirectUri;
            if (redirectUri == null || !Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
            {
                redirectUri ??= "authentication/login-callback";
                options.ProviderOptions.RedirectUri = navigationManager
                    .ToAbsoluteUri(redirectUri).AbsoluteUri;
            }

            var silentRedirectUri = options.ProviderOptions.SilentRedirectUri;
            if (silentRedirectUri == null || !Uri.TryCreate(silentRedirectUri, UriKind.Absolute, out _))
            {
                options.ProviderOptions.SilentRedirectUri =
                    navigationManager.ToAbsoluteUri("/authentication/silent-redirect").AbsoluteUri;
            }

            options.ProviderOptions.SilentRequestTimeoutInSeconds = 45;
            options.ProviderOptions.IncludeIdTokenInSilentRenew = true;

            var postLogoutRedirectUri = options.ProviderOptions.PostLogoutRedirectUri;
            if (postLogoutRedirectUri == null || !Uri.TryCreate(postLogoutRedirectUri, UriKind.Absolute, out _))
            {
                options.ProviderOptions.PostLogoutRedirectUri =
                    navigationManager.ToAbsoluteUri("/authentication/logout-callback").AbsoluteUri;
            }

            options.ProviderOptions.IncludeIdTokenInSilentSignout = true;
            options.ProviderOptions.MaxSilentRenewTimeoutRetries = 1;
            options.ProviderOptions.ResponseMode = "fragment";
            options.ProviderOptions.DisablePKCE = false;
            options.ProviderOptions.LoadUserInfo = true;
            options.ProviderOptions.AccessTokenExpiringNotificationTimeInSeconds = 30;
            options.ProviderOptions.AutomaticSilentRenew = true;
        }
    }
}
