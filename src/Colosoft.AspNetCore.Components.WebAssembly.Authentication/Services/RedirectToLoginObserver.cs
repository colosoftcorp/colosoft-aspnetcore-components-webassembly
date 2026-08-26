using Microsoft.AspNetCore.Components;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal class RedirectToLoginObserver(
    NavigationManager navigationManager,
    IRemoteAuthenticationPathsProvider remoteApplicationPathsProvider,
    string loginPath)
    : IRemoteAuthenticationServiceObserver
{
    private string LoginPath
    {
        get => loginPath ?? remoteApplicationPathsProvider.ApplicationPaths.LogInPath;
    }

    private string GetReturnUrl()
    {
        return navigationManager.BaseUri;
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
        navigationManager.NavigateToLogin(
            this.LoginPath,
            new InteractiveRequestOptions
            {
                Interaction = InteractionType.SignIn,
                ReturnUrl = this.GetReturnUrl(),
            });
    }
}
