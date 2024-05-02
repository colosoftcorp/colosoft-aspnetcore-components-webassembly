using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public interface IRemoteAuthenticationService<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>
    where TRemoteAuthenticationState : RemoteAuthenticationState
{
    Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignInAsync(RemoteAuthenticationContext<TRemoteAuthenticationState> context);

    Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignInAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context);

    Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> SignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context);

    Task<RemoteAuthenticationResult<TRemoteAuthenticationState>> CompleteSignOutAsync(
        RemoteAuthenticationContext<TRemoteAuthenticationState> context);
}
