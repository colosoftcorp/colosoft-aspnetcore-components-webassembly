using Microsoft.AspNetCore.Components;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal class RedirectToLoginObserver(
    NavigationManager navigationManager,
    IRemoteAuthenticationPathsProvider remoteApplicationPathsProvider,
    string loginPath)
    : IRemoteAuthenticationServiceObserver
{
    private int redirecting;

    private string LoginPath
    {
        get => loginPath ?? remoteApplicationPathsProvider.ApplicationPaths.LogInPath;
    }

    private string GetReturnUrl()
    {
        var current = navigationManager.Uri;
        var loginUri = navigationManager.ToAbsoluteUri(this.LoginPath).AbsoluteUri;

        if (string.IsNullOrEmpty(current) ||
            current.StartsWith(loginUri, StringComparison.OrdinalIgnoreCase) ||
            current.Contains("/authentication/", StringComparison.OrdinalIgnoreCase))
        {
            return navigationManager.BaseUri;
        }

        return current;
    }

    public Task AccessTokenExpired(CancellationToken cancellationToken)
    {
        this.NavigateToLogin();
        return Task.CompletedTask;
    }

    public Task OnAccessTokenExpiring(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task OnSilentRenewError(string error, CancellationToken cancellationToken)
    {
        this.NavigateToLogin();
        return Task.CompletedTask;
    }

    public Task OnUserLoaded(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task OnUserSessionChanged(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task OnUserSignOut(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task OnUserUnloaded(CancellationToken cancellationToken) => Task.CompletedTask;

    private void NavigateToLogin()
    {
        if (Interlocked.Exchange(ref this.redirecting, 1) == 1)
        {
            return;
        }

        navigationManager.NavigateToLogin(
            this.LoginPath,
            new InteractiveRequestOptions
            {
                Interaction = InteractionType.SignIn,
                ReturnUrl = this.GetReturnUrl(),
            });
    }
}
