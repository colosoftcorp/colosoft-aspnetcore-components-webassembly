using Colosoft.AspNetCore.Components.WebAssembly.Authentication.Services;
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
    IAccessTokenProvider,
    IRemoteAuthenticationServiceListener,
    IDisposable
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TProviderOptions : new()
    where TAccount : RemoteUserAccount
{
    private static readonly TimeSpan UserCacheRefreshInterval = TimeSpan.FromSeconds(60);
    private readonly RemoteAuthenticationServiceJavaScriptLoggingOptions loggingOptions;
    private readonly AuthenticationServiceCallbacks callbacks;
    private readonly DotNetObjectReference<AuthenticationServiceCallbacks> callbacksObjectReference;
    private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private readonly List<IRemoteAuthenticationServiceObserver> observers = new List<IRemoteAuthenticationServiceObserver>();

    private bool initialized;

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
        ILogger<RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>>? logger,
        IServiceProvider? serviceProvider = null)
    {
        this.callbacks = new AuthenticationServiceCallbacks();
        this.callbacksObjectReference = DotNetObjectReference.Create(this.callbacks);
        this.ConfigureCallbacks();

        this.JsRuntime = jsRuntime;
        this.Navigation = navigation;
        this.AccountClaimsPrincipalFactory = accountClaimsPrincipalFactory;
        this.Options = options.Value;
        this.loggingOptions = new RemoteAuthenticationServiceJavaScriptLoggingOptions
        {
            DebugEnabled = logger?.IsEnabled(LogLevel.Debug) ?? false,
            TraceEnabled = logger?.IsEnabled(LogLevel.Trace) ?? false,
        };

        this.observers.AddRange(this.Options.GetObservers(serviceProvider));
    }

    ~RemoteAuthenticationService() => this.Dispose(false);

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!(await this.HasValidAccessToken()))
        {
            return new AuthenticationState(new ClaimsPrincipal());
        }

        var user = await this.GetUser(useCache: true);
        return new AuthenticationState(user);
    }

    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JSInvokeWithContextAsync<RemoteAuthenticationContext<TRemoteAuthenticationState>, RemoteAuthenticationResult<TRemoteAuthenticationState>>(
            "AuthenticationService.signIn",
            context);

        if (result.ErrorMessage == "Failed to fetch")
        {
            result.ErrorMessage = Properties.Resources.SignInFailedToFetchMessage;
        }

        await this.UpdateUserOnSuccess(result);

        return result;
    }

    public virtual async Task<RemoteSignInUrlResult<TRemoteAuthenticationState>> CreateSignInUrl(RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JSInvokeWithContextAsync<RemoteAuthenticationContext<TRemoteAuthenticationState>, RemoteSignInUrlResult<TRemoteAuthenticationState>>(
            "AuthenticationService.createSignInUrl",
            context);

        return result;
    }

    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>(
            "AuthenticationService.completeSignIn",
            context.Url);

        await this.UpdateUserOnSuccess(result);

        return result;
    }

    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JSInvokeWithContextAsync<RemoteAuthenticationContext<TRemoteAuthenticationState>, RemoteAuthenticationResult<TRemoteAuthenticationState>>(
            "AuthenticationService.signOut",
            context);

        await this.UpdateUserOnSuccess(result);

        return result;
    }

    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>(
            "AuthenticationService.completeSignOut",
            context.Url);

        await this.UpdateUserOnSuccess(result);

        return result;
    }

    public virtual async Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SilentRedirectAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context)
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<RemoteAuthenticationResult<TRemoteAuthenticationState>>(
            "AuthenticationService.silentRedirect",
            context);

        await this.UpdateUserOnSuccess(result);

        return result;
    }

    public virtual async ValueTask<bool> HasValidAccessToken()
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<HasValidAccessTokenResult>(
            "AuthenticationService.hasValidAccessToken");

        return (result?.HasValidAccessToken).GetValueOrDefault();
    }

    [DynamicDependency(JsonSerialized, typeof(HasValidAccessTokenRequestOptions))]
    public virtual async ValueTask<bool> HasValidAccessToken(HasValidAccessTokenRequestOptions options)
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<HasValidAccessTokenResult>(
            "AuthenticationService.hasValidAccessToken",
            options);

        return (result?.HasValidAccessToken).GetValueOrDefault();
    }

    public virtual async ValueTask<AccessTokenResult> RequestAccessToken()
    {
        await this.EnsureAuthService();
        var result = await this.JsRuntime.InvokeAsync<InternalAccessTokenResult>(
            "AuthenticationService.getAccessToken");

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
        var result = await this.JsRuntime.InvokeAsync<InternalAccessTokenResult>(
            "AuthenticationService.getAccessToken",
            options);

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
            await this.JsRuntime.InvokeVoidAsync(
                "AuthenticationService.init",
                this.Options.ProviderOptions,
                this.loggingOptions,
                this.callbacksObjectReference);

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

        static async Task<AuthenticationState> UpdateAuthenticationState(Task<ClaimsPrincipal> futureUser) =>
            new AuthenticationState(await futureUser);
    }

    private async Task NotifyObserver(Func<IRemoteAuthenticationServiceObserver, Task> callback)
    {
        IEnumerable<IRemoteAuthenticationServiceObserver> observersCopy;
        lock (this.observers)
        {
            observersCopy = this.observers.ToArray();
        }

        foreach (var observer in observersCopy)
        {
            await callback(observer);
        }
    }

    private void ConfigureCallbacks()
    {
        this.callbacks.UserLoaded += async () =>
        {
            await this.NotifyObserver(observer => observer.OnUserLoaded(this.cancellationTokenSource.Token));
        };

        this.callbacks.UserUnloaded += async () =>
        {
            await this.NotifyObserver(observer => observer.OnUserUnloaded(this.cancellationTokenSource.Token));
        };

        this.callbacks.AccessTokenExpiring += async () =>
        {
            await this.NotifyObserver(observer => observer.OnAccessTokenExpiring(this.cancellationTokenSource.Token));
        };

        this.callbacks.AccessTokenExpired += async () =>
        {
            await this.NotifyObserver(observer => observer.AccessTokenExpired(this.cancellationTokenSource.Token));
        };

        this.callbacks.SilentRenewError += async (error) =>
        {
            await this.NotifyObserver(observer => observer.OnSilentRenewError(error, this.cancellationTokenSource.Token));
        };

        this.callbacks.UserSignOut += async () =>
        {
            await this.NotifyObserver(observer => observer.OnUserSignOut(this.cancellationTokenSource.Token));
        };

        this.callbacks.UserSessionChanged += async () =>
        {
            await this.NotifyObserver(observer => observer.OnUserSessionChanged(this.cancellationTokenSource.Token));
        };
    }

    public void Add(IRemoteAuthenticationServiceObserver observer)
    {
        lock (this.observers)
        {
            if (!this.observers.Contains(observer))
            {
                this.observers.Add(observer);
            }
        }
    }

    public bool Remove(IRemoteAuthenticationServiceObserver observer)
    {
        lock (this.observers)
        {
            return this.observers.Remove(observer);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        this.callbacksObjectReference.Dispose();

        if (!this.cancellationTokenSource.IsCancellationRequested)
        {
            this.cancellationTokenSource.Cancel();
        }

        this.cancellationTokenSource.Dispose();
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
}
