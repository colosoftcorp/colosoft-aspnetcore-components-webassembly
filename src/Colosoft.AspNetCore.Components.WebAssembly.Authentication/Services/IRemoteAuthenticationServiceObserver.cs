namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public interface IRemoteAuthenticationServiceObserver
{
    Task OnUserLoaded(CancellationToken cancellationToken);

    Task OnUserUnloaded(CancellationToken cancellationToken);

    Task OnAccessTokenExpiring(CancellationToken cancellationToken);

    Task AccessTokenExpired(CancellationToken cancellationToken);

    Task OnSilentRenewError(string error, CancellationToken cancellationToken);

    Task OnUserSignOut(CancellationToken cancellationToken);

    Task OnUserSessionChanged(CancellationToken cancellationToken);
}
