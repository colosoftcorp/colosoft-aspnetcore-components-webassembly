using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
public class AuthorizationMessageHandler : DelegatingHandler, IDisposable
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
#pragma warning restore CA1063 // Implement IDisposable Correctly
{
    private readonly IAccessTokenProvider provider;
    private readonly NavigationManager navigation;
    private readonly AuthenticationStateChangedHandler? authenticationStateChangedHandler;
    private AccessToken? lastToken;
    private AuthenticationHeaderValue? cachedHeader;
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
    private Uri[]? authorizedUris;
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly
    private AccessTokenRequestOptions? tokenOptions;

    public AuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation)
    {
        this.provider = provider;
        this.navigation = navigation;

        // Invalidate the cached _lastToken when the authentication state changes
        if (this.provider is AuthenticationStateProvider authStateProvider)
        {
            this.authenticationStateChangedHandler = _ => { this.lastToken = null; };
            authStateProvider.AuthenticationStateChanged += this.authenticationStateChangedHandler;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        if (this.authorizedUris == null)
        {
            throw new InvalidOperationException($"The '{nameof(AuthorizationMessageHandler)}' is not configured. " +
                $"Call '{nameof(AuthorizationMessageHandler.ConfigureHandler)}' and provide a list of endpoint urls to attach the token to.");
        }

        if (request.RequestUri != null && this.authorizedUris.Any(uri => uri.IsBaseOf(request.RequestUri)))
        {
            if (this.lastToken == null || now >= this.lastToken.Expires.AddMinutes(-5))
            {
                var tokenResult = this.tokenOptions != null ?
                    await this.provider.RequestAccessToken(this.tokenOptions) :
                    await this.provider.RequestAccessToken();

                if (tokenResult.TryGetToken(out var token))
                {
                    this.lastToken = token;
                    this.cachedHeader = new AuthenticationHeaderValue("Bearer", this.lastToken.Value);
                }
                else
                {
                    throw new AccessTokenNotAvailableException(this.navigation, tokenResult, this.tokenOptions?.Scopes);
                }
            }

            request.Headers.Authorization = this.cachedHeader;
        }

        return await base.SendAsync(request, cancellationToken);
    }

    public AuthorizationMessageHandler ConfigureHandler(
        IEnumerable<string> authorizedUrls,
        IEnumerable<string>? scopes = null,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? returnUrl = null)
    {
        if (this.authorizedUris != null)
        {
            throw new InvalidOperationException("Handler already configured.");
        }

        ArgumentNullException.ThrowIfNull(authorizedUrls);

        var uris = authorizedUrls.Select(uri => new Uri(uri, UriKind.Absolute)).ToArray();
        if (uris.Length == 0)
        {
            throw new ArgumentException("At least one URL must be configured.", nameof(authorizedUrls));
        }

        this.authorizedUris = uris;
        var scopesList = scopes?.ToArray();
        if (scopesList != null || returnUrl != null)
        {
            this.tokenOptions = new AccessTokenRequestOptions
            {
                Scopes = scopesList,
                ReturnUrl = returnUrl,
            };
        }

        return this;
    }

#pragma warning disable CA1063 // Implement IDisposable Correctly
    void IDisposable.Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        if (this.provider is AuthenticationStateProvider authStateProvider)
        {
            authStateProvider.AuthenticationStateChanged -= this.authenticationStateChangedHandler;
        }

        this.Dispose(disposing: true);
    }
}
