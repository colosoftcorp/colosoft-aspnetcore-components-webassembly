using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticationService<
    [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState,
    [DynamicallyAccessedMembers(JsonSerialized)] TAccount,
    [DynamicallyAccessedMembers(JsonSerialized)] TProviderOptions> :
    AuthenticationStateProvider,
    IRemoteAuthenticationService<TRemoteAuthenticationState>,
    IAccessTokenProvider
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TProviderOptions : new()
    where TAccount : RemoteUserAccount
{
#pragma warning disable S2743 // Static fields should not be used in generic types
    private static readonly TimeSpan UserCacheRefreshInterval = TimeSpan.FromSeconds(60);
#pragma warning restore S2743 // Static fields should not be used in generic types
    private readonly RemoteAuthenticationServiceJavaScriptLoggingOptions loggingOptions;

    private bool initialized;

    // This defaults to 1/1/1970
    private DateTimeOffset userLastCheck = DateTimeOffset.FromUnixTimeSeconds(0);
    private ClaimsPrincipal cachedUser = new ClaimsPrincipal(new ClaimsIdentity());

    protected IJSRuntime JsRuntime { get; }

    protected NavigationManager Navigation { get; }

    protected AccountClaimsPrincipalFactory<TAccount> AccountClaimsPrincipalFactory { get; }

    protected RemoteAuthenticationOptions<TProviderOptions> Options { get; }

    public RemoteAuthenticationService(
        IJSRuntime jsRuntime,
        IOptionsSnapshot<RemoteAuthenticationOptions<TProviderOptions>> options,
        NavigationManager navigation,
        AccountClaimsPrincipalFactory<TAccount> accountClaimsPrincipalFactory,
        ILogger<RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>>? logger)
    {
        this.JsRuntime = jsRuntime;
        this.Navigation = navigation;
        this.AccountClaimsPrincipalFactory = accountClaimsPrincipalFactory;
        this.Options = options.Value;
        this.loggingOptions = new RemoteAuthenticationServiceJavaScriptLoggingOptions
        {
            DebugEnabled = logger?.IsEnabled(LogLevel.Debug) ?? false,
            TraceEnabled = logger?.IsEnabled(LogLevel.Trace) ?? false,
        };
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync() => new AuthenticationState(await this.GetUser(useCache: true));

    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JSInvokeWithContextAsync<RemoteAuthenticationContext<TRemoteAuthenticationState>, RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signIn", context);
        await this.UpdateUserOnSuccess(result);

        return result;
    }

    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.completeSignIn", context.Url);
        await this.UpdateUserOnSuccess(result);

        return result;
    }

    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JSInvokeWithContextAsync<RemoteAuthenticationContext<TRemoteAuthenticationState>, RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.signOut", context);
        await this.UpdateUserOnSuccess(result);

        return result;
    }

    /// <inheritdoc />
    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>("AuthenticationService.completeSignOut", context.Url);
        await this.UpdateUserOnSuccess(result);

        return result;
    }

    public virtual async ValueTask<AccessTokenResult> RequestAccessToken()
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken");

        var requestOptions = result.Status == AccessTokenResultStatus.RequiresRedirect
            ? new InteractiveRequestOptions
            {
                Interaction = InteractionType.GetToken,
                ReturnUrl = this.GetReturnUrl(null),
            }
            : null;

        return new AccessTokenResult(
            result.Status,
            result.Token,
            result.Status == AccessTokenResultStatus.RequiresRedirect ? this.Options.AuthenticationPaths.LogInPath : null,
            requestOptions);
    }

    [DynamicDependency(JsonSerialized, typeof(AccessToken))]
    [DynamicDependency(JsonSerialized, typeof(AccessTokenRequestOptions))]

    public virtual async ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<InternalAccessTokenResult>("AuthenticationService.getAccessToken", options);

        var requestOptions = result.Status == AccessTokenResultStatus.RequiresRedirect
            ? new InteractiveRequestOptions
            {
                Interaction = InteractionType.GetToken,
                ReturnUrl = this.GetReturnUrl(options.ReturnUrl),
                Scopes = options.Scopes ?? Array.Empty<string>(),
            }
            : null;

        return new AccessTokenResult(
            result.Status,
            result.Token,
            result.Status == AccessTokenResultStatus.RequiresRedirect ? this.Options.AuthenticationPaths.LogInPath : null,
            requestOptions);
    }

    // JSRuntime.InvokeAsync does not properly annotate all arguments with DynamicallyAccessedMembersAttribute. https://github.com/dotnet/aspnetcore/issues/39839
    // Calling JsRuntime.InvokeAsync directly results allows the RemoteAuthenticationContext.State getter to be trimmed. https://github.com/dotnet/aspnetcore/issues/49956
    private ValueTask<TResult> JSInvokeWithContextAsync<[DynamicallyAccessedMembers(JsonSerialized)] TContext, [DynamicallyAccessedMembers(JsonSerialized)] TResult>(
        string identifier, TContext context) => this.JsRuntime.InvokeAsync<TResult>(identifier, context);

    private string GetReturnUrl(string? customReturnUrl) =>
        customReturnUrl != null ? this.Navigation.ToAbsoluteUri(customReturnUrl).AbsoluteUri : this.Navigation.Uri;

    private async Task<ClaimsPrincipal> GetUser(bool useCache = false)
    {
        var now = DateTimeOffset.Now;
        if (useCache && now < this.userLastCheck + UserCacheRefreshInterval)
        {
            return this.cachedUser;
        }

        this.cachedUser = await this.GetAuthenticatedUser();
        this.userLastCheck = now;

        return this.cachedUser;
    }

    protected internal virtual async ValueTask<ClaimsPrincipal> GetAuthenticatedUser()
    {
        await this.EnsureAuthService();
        var account = await this.JsRuntime.InvokeAsync<TAccount>("AuthenticationService.getUser");
        var user = await this.AccountClaimsPrincipalFactory.CreateUserAsync(account, this.Options.UserOptions);

        return user;
    }

    [DynamicDependency(JsonSerialized, typeof(RemoteAuthenticationServiceJavaScriptLoggingOptions))]
    private async ValueTask EnsureAuthService()
    {
        if (!this.initialized)
        {
            await this.JsRuntime.InvokeVoidAsync("AuthenticationService.init", this.Options.ProviderOptions, this.loggingOptions);
            this.initialized = true;
        }
    }

    private async Task UpdateUserOnSuccess(RemoteAuthenticationResult<TRemoteAuthenticationState> result)
    {
        if (result.Status == RemoteAuthenticationStatus.Success)
        {
            var getUserTask = this.GetUser();
            await getUserTask;
            this.UpdateUser(getUserTask);
        }
    }

    private void UpdateUser(Task<ClaimsPrincipal> task)
    {
        this.NotifyAuthenticationStateChanged(UpdateAuthenticationState(task));

        static async Task<AuthenticationState> UpdateAuthenticationState(Task<ClaimsPrincipal> futureUser) => new AuthenticationState(await futureUser);
    }
}
