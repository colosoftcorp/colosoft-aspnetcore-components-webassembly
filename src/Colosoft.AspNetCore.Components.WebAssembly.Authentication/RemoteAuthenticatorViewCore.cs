using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public partial class RemoteAuthenticatorViewCore<[DynamicallyAccessedMembers(JsonSerialized)] TAuthenticationState> : ComponentBase
    where TAuthenticationState : RemoteAuthenticationState
{
#pragma warning disable S2743 // Static fields should not be used in generic types
    private static readonly NavigationOptions AuthenticationNavigationOptions =
#pragma warning restore S2743 // Static fields should not be used in generic types
        new() { ReplaceHistoryEntry = true, ForceLoad = false };

    private RemoteAuthenticationApplicationPathsOptions? applicationPaths;
    private string? action;
    private string? lastHandledAction;
    private InteractiveRequestOptions? cachedRequest;

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public string? Action
#pragma warning restore BL0007 // Component parameters should be auto properties
    {
        get => this.action;
        set => this.action = value?.ToLowerInvariant();
    }

    [Parameter]
    public TAuthenticationState AuthenticationState { get; set; } = default!;

    [Parameter]
    public RenderFragment? LoggingIn { get; set; } = DefaultLogInFragment;

    [Parameter]
    public RenderFragment? Registering { get; set; }

    [Parameter]
    public RenderFragment? UserProfile { get; set; }

    [Parameter]
    public RenderFragment CompletingLoggingIn { get; set; } = DefaultLogInCallbackFragment;

    [Parameter]
    public RenderFragment<string?> LogInFailed { get; set; } = DefaultLogInFailedFragment;

    [Parameter]
    public RenderFragment LogOut { get; set; } = DefaultLogOutFragment;

    [Parameter]
    public RenderFragment CompletingLogOut { get; set; } = DefaultLogOutCallbackFragment;

    [Parameter]
    public RenderFragment<string?> LogOutFailed { get; set; } = DefaultLogOutFailedFragment;

    [Parameter]
    public RenderFragment LogOutSucceeded { get; set; } = DefaultLoggedOutFragment;

    [Parameter]
    public EventCallback<TAuthenticationState> OnLogInSucceeded { get; set; }

    [Parameter]
    public EventCallback<TAuthenticationState> OnLogOutSucceeded { get; set; }

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public RemoteAuthenticationApplicationPathsOptions ApplicationPaths
#pragma warning restore BL0007 // Component parameters should be auto properties
    {
        get => this.applicationPaths ?? this.RemoteApplicationPathsProvider.ApplicationPaths;
        set => this.applicationPaths = value!;
    }

    [Inject]
    internal NavigationManager Navigation { get; set; } = default!;

    [Inject]
    internal IRemoteAuthenticationService<TAuthenticationState> AuthenticationService { get; set; } = default!;

    [Inject]
    internal IRemoteAuthenticationPathsProvider RemoteApplicationPathsProvider { get; set; } = default!;

    [Inject]
    internal AuthenticationStateProvider AuthenticationProvider { get; set; } = default!;

#pragma warning disable CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility
    [Inject]
    internal SignOutSessionStateManager SignOutManager { get; set; } = default!;
#pragma warning restore CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility

    [Inject]
    internal ILogger<RemoteAuthenticatorViewCore<TAuthenticationState>> Logger { get; set; } = default!;

    private static void DefaultLogInFragment(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Checking login state...");
        builder.CloseElement();
    }

    private static void RegisterNotSupportedFragment(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Registration is not supported.");
        builder.CloseElement();
    }

    private static void ProfileNotSupportedFragment(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Editing the profile is not supported.");
        builder.CloseElement();
    }

    private static void DefaultLogInCallbackFragment(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Completing login...");
        builder.CloseElement();
    }

    private static RenderFragment DefaultLogInFailedFragment(string? message)
    {
        return builder =>
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "There was an error trying to log you in: '");
            builder.AddContent(2, message);
            builder.AddContent(3, "'");
            builder.CloseElement();
        };
    }

    private static void DefaultLogOutFragment(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Processing logout...");
        builder.CloseElement();
    }

    private static void DefaultLogOutCallbackFragment(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Processing logout callback...");
        builder.CloseElement();
    }

    private static RenderFragment DefaultLogOutFailedFragment(string? message)
    {
        return builder =>
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "There was an error trying to log you out: '");
            builder.AddContent(2, message);
            builder.AddContent(3, "'");
            builder.CloseElement();
        };
    }

    private static void DefaultLoggedOutFragment(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "You are logged out.");
        builder.CloseElement();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);
        switch (this.Action)
        {
            case RemoteAuthenticationActions.Profile:
                builder.AddContent(0, this.UserProfile);
                break;
            case RemoteAuthenticationActions.Register:
                builder.AddContent(0, this.Registering);
                break;
            case RemoteAuthenticationActions.LogIn:
                builder.AddContent(0, this.LoggingIn);
                break;
            case RemoteAuthenticationActions.LogInCallback:
                builder.AddContent(0, this.CompletingLoggingIn);
                break;
            case RemoteAuthenticationActions.LogInFailed:
                builder.AddContent(0, this.LogInFailed(this.Navigation.HistoryEntryState));
                break;
            case RemoteAuthenticationActions.LogOut:
                builder.AddContent(0, this.LogOut);
                break;
            case RemoteAuthenticationActions.LogOutCallback:
                builder.AddContent(0, this.CompletingLogOut);
                break;
            case RemoteAuthenticationActions.LogOutFailed:
                builder.AddContent(0, this.LogOutFailed(this.Navigation.HistoryEntryState));
                break;
            case RemoteAuthenticationActions.LogOutSucceeded:
                builder.AddContent(0, this.LogOutSucceeded);
                break;
            default:
                throw new InvalidOperationException($"Invalid action '{this.Action}'.");
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (this.lastHandledAction == this.Action)
        {
            // Avoid processing the same action more than once.
            return;
        }

        this.lastHandledAction = this.Action;
        Log.ProcessingAuthenticatorAction(this.Logger, this.Action);
        switch (this.Action)
        {
            case RemoteAuthenticationActions.LogIn:
                await this.ProcessLogIn(this.GetReturnUrl(state: null));
                break;
            case RemoteAuthenticationActions.LogInCallback:
                await this.ProcessLogInCallback();
                break;
            case RemoteAuthenticationActions.LogInFailed:
                break;
            case RemoteAuthenticationActions.Profile:
                if (this.ApplicationPaths.RemoteProfilePath == null)
                {
                    this.UserProfile ??= ProfileNotSupportedFragment;
                }
                else
                {
                    this.UserProfile ??= this.LoggingIn;
                    this.RedirectToProfile();
                }

                break;
            case RemoteAuthenticationActions.Register:
                if (this.ApplicationPaths.RemoteRegisterPath == null)
                {
                    this.Registering ??= RegisterNotSupportedFragment;
                }
                else
                {
                    this.Registering ??= this.LoggingIn;
                    this.RedirectToRegister();
                }

                break;
            case RemoteAuthenticationActions.LogOut:
                await this.ProcessLogOut(this.GetReturnUrl(state: null, this.ApplicationPaths.LogOutSucceededPath));
                break;
            case RemoteAuthenticationActions.LogOutCallback:
                await this.ProcessLogOutCallback();
                break;
            case RemoteAuthenticationActions.LogOutFailed:
                break;
            case RemoteAuthenticationActions.LogOutSucceeded:
                break;
            default:
                throw new InvalidOperationException($"Invalid action '{this.Action}'.");
        }
    }

    private async Task ProcessLogIn(string returnUrl)
    {
        this.AuthenticationState.ReturnUrl = returnUrl;
        var interactiveRequest = this.GetCachedNavigationState();
        var result = await this.AuthenticationService.SignInAsync(new RemoteAuthenticationContext<TAuthenticationState>
        {
            State = this.AuthenticationState,
            InteractiveRequest = interactiveRequest,
        });

        switch (result.Status)
        {
            case RemoteAuthenticationStatus.Redirect:
                Log.LoginRequiresRedirect(this.Logger);
                break;
            case RemoteAuthenticationStatus.Success:
                Log.LoginCompletedSuccessfully(this.Logger);
                if (this.OnLogInSucceeded.HasDelegate)
                {
                    Log.InvokingLoginCompletedCallback(this.Logger);
                    await this.OnLogInSucceeded.InvokeAsync(result.State);
                }

                var redirectUrl = this.GetReturnUrl(result.State, returnUrl);
                Log.NavigatingToUrl(this.Logger, redirectUrl);
                this.Navigation.NavigateTo(redirectUrl, AuthenticationNavigationOptions);
                break;
            case RemoteAuthenticationStatus.Failure:
                Log.LoginFailed(this.Logger, result.ErrorMessage!);
                Log.NavigatingToUrl(this.Logger, this.ApplicationPaths.LogInFailedPath);
#pragma warning disable SA1101 // Prefix local calls with this
                this.Navigation.NavigateTo(
                    this.ApplicationPaths.LogInFailedPath,
                    AuthenticationNavigationOptions with { HistoryEntryState = result.ErrorMessage });
#pragma warning restore SA1101 // Prefix local calls with this
                break;
            case RemoteAuthenticationStatus.OperationCompleted:
            default:
                throw new InvalidOperationException($"Invalid authentication result status '{result.Status}'.");
        }
    }

    private async Task ProcessLogInCallback()
    {
        var result = await this.AuthenticationService.CompleteSignInAsync(
            new RemoteAuthenticationContext<TAuthenticationState> { Url = this.Navigation.Uri });
        switch (result.Status)
        {
            case RemoteAuthenticationStatus.Redirect:
                // There should not be any redirects as the only time CompleteSignInAsync finishes
                // is when we are doing a redirect sign in flow.
                throw new InvalidOperationException("Should not redirect.");
            case RemoteAuthenticationStatus.Success:
                Log.LoginRedirectCompletedSuccessfully(this.Logger);
                if (this.OnLogInSucceeded.HasDelegate)
                {
                    Log.InvokingLoginCompletedCallback(this.Logger);
                    await this.OnLogInSucceeded.InvokeAsync(result.State);
                }

                var redirectUrl = this.GetReturnUrl(result.State);
                Log.NavigatingToUrl(this.Logger, redirectUrl);
                this.Navigation.NavigateTo(redirectUrl, AuthenticationNavigationOptions);
                break;
            case RemoteAuthenticationStatus.OperationCompleted:
                break;
            case RemoteAuthenticationStatus.Failure:
                Log.LoginCallbackFailed(this.Logger, result.ErrorMessage!);
                Log.NavigatingToUrl(this.Logger, this.ApplicationPaths.LogInFailedPath);
#pragma warning disable SA1101 // Prefix local calls with this
                this.Navigation.NavigateTo(
                    this.ApplicationPaths.LogInFailedPath,
                    AuthenticationNavigationOptions with { HistoryEntryState = result.ErrorMessage });
#pragma warning restore SA1101 // Prefix local calls with this
                break;
            default:
                throw new InvalidOperationException($"Invalid authentication result status '{result.Status}'.");
        }
    }

    private async Task ProcessLogOut(string returnUrl)
    {
        if ((this.Navigation.HistoryEntryState != null && !this.ValidateSignOutRequestState()) ||

            // For backcompat purposes, keep SignOutManager working, even though we now use the history.state for this.
            (this.Navigation.HistoryEntryState == null && !await this.SignOutManager.ValidateSignOutState()))
        {
            Log.LogoutOperationInitiatedExternally(this.Logger);
#pragma warning disable SA1101 // Prefix local calls with this
            this.Navigation.NavigateTo(this.ApplicationPaths.LogOutFailedPath, AuthenticationNavigationOptions with { HistoryEntryState = "The logout was not initiated from within the page." });
#pragma warning restore SA1101 // Prefix local calls with this
            return;
        }

        this.AuthenticationState.ReturnUrl = returnUrl;

        var state = await this.AuthenticationProvider.GetAuthenticationStateAsync();
        var isauthenticated = state.User.Identity?.IsAuthenticated ?? false;
        if (isauthenticated)
        {
            var interactiveRequest = this.GetCachedNavigationState();
            var result = await this.AuthenticationService.SignOutAsync(new RemoteAuthenticationContext<TAuthenticationState>
            {
                State = this.AuthenticationState,
                InteractiveRequest = interactiveRequest,
            });
            switch (result.Status)
            {
                case RemoteAuthenticationStatus.Redirect:
                    Log.LogoutRequiresRedirect(this.Logger);
                    break;
                case RemoteAuthenticationStatus.Success:
                    Log.LogoutCompletedSuccessfully(this.Logger);
                    if (this.OnLogOutSucceeded.HasDelegate)
                    {
                        Log.InvokingLogoutCompletedCallback(this.Logger);
                        await this.OnLogOutSucceeded.InvokeAsync(result.State);
                    }

                    Log.NavigatingToUrl(this.Logger, returnUrl);
                    this.Navigation.NavigateTo(returnUrl, AuthenticationNavigationOptions);
                    break;
                case RemoteAuthenticationStatus.OperationCompleted:
                    break;
                case RemoteAuthenticationStatus.Failure:
                    Log.LogoutFailed(this.Logger, result.ErrorMessage!);
                    Log.NavigatingToUrl(this.Logger, this.ApplicationPaths.LogOutFailedPath);
#pragma warning disable SA1101 // Prefix local calls with this
                    this.Navigation.NavigateTo(this.ApplicationPaths.LogOutFailedPath, AuthenticationNavigationOptions with { HistoryEntryState = result.ErrorMessage });
#pragma warning restore SA1101 // Prefix local calls with this
                    break;
                default:
                    throw new InvalidOperationException($"Invalid authentication result status.");
            }
        }
        else
        {
            Log.NavigatingToUrl(this.Logger, returnUrl);
            this.Navigation.NavigateTo(returnUrl, AuthenticationNavigationOptions);
        }
    }

    private async Task ProcessLogOutCallback()
    {
        var result = await this.AuthenticationService.CompleteSignOutAsync(new RemoteAuthenticationContext<TAuthenticationState> { Url = this.Navigation.Uri });
        switch (result.Status)
        {
            case RemoteAuthenticationStatus.Redirect:
                // There should not be any redirects as the only time completeAuthentication finishes
                // is when we are doing a redirect sign in flow.
                throw new InvalidOperationException("Should not redirect.");
            case RemoteAuthenticationStatus.Success:
                Log.LogoutRedirectCompletedSuccessfully(this.Logger);
                if (this.OnLogOutSucceeded.HasDelegate)
                {
                    Log.InvokingLogoutCompletedCallback(this.Logger);
                    await this.OnLogOutSucceeded.InvokeAsync(result.State);
                }

                var redirectUrl = this.GetReturnUrl(result.State, this.ApplicationPaths.LogOutSucceededPath);
                Log.NavigatingToUrl(this.Logger, redirectUrl);
                this.Navigation.NavigateTo(redirectUrl, AuthenticationNavigationOptions);
                break;
            case RemoteAuthenticationStatus.OperationCompleted:
                break;
            case RemoteAuthenticationStatus.Failure:
                Log.LogoutCallbackFailed(this.Logger, result.ErrorMessage!);
#pragma warning disable SA1101 // Prefix local calls with this
                this.Navigation.NavigateTo(this.ApplicationPaths.LogOutFailedPath, AuthenticationNavigationOptions with { HistoryEntryState = result.ErrorMessage });
#pragma warning restore SA1101 // Prefix local calls with this
                break;
            default:
                throw new InvalidOperationException($"Invalid authentication result status.");
        }
    }

    private string GetReturnUrl(TAuthenticationState? state, string? defaultReturnUrl = null)
    {
        if (state?.ReturnUrl != null)
        {
            return state.ReturnUrl;
        }

        var fromNavigationState = this.GetCachedNavigationState()?.ReturnUrl;

        return fromNavigationState ?? defaultReturnUrl ?? this.Navigation.BaseUri;
    }

    private bool ValidateSignOutRequestState()
    {
        return this.GetCachedNavigationState()?.Interaction == InteractionType.SignOut;
    }

    private InteractiveRequestOptions? GetCachedNavigationState()
    {
        if (this.cachedRequest != null)
        {
            return this.cachedRequest;
        }

        if (string.IsNullOrEmpty(this.Navigation.HistoryEntryState))
        {
            return null;
        }

        this.cachedRequest = InteractiveRequestOptions.FromState(this.Navigation.HistoryEntryState);
        return this.cachedRequest;
    }

    private void RedirectToRegister()
    {
        var loginUrl = this.Navigation.ToAbsoluteUri(this.ApplicationPaths.LogInPath).PathAndQuery;
        var registerUrl = this.Navigation.ToAbsoluteUri(this.ApplicationPaths.RemoteRegisterPath).AbsoluteUri;
        var navigationUrl = this.Navigation.GetUriWithQueryParameters(
            registerUrl,
            new Dictionary<string, object?> { ["returnUrl"] = loginUrl });

#pragma warning disable SA1101 // Prefix local calls with this
        this.Navigation.NavigateTo(navigationUrl, AuthenticationNavigationOptions with { ForceLoad = true });
#pragma warning restore SA1101 // Prefix local calls with this
    }

    private void RedirectToProfile() =>
        this.Navigation.NavigateTo(this.Navigation.ToAbsoluteUri(this.ApplicationPaths.RemoteProfilePath).AbsoluteUri, new NavigationOptions { ReplaceHistoryEntry = true, ForceLoad = true });
}
