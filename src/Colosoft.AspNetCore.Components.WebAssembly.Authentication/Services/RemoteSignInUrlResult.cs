namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication.Services;

public class RemoteSignInUrlResult<TRemoteAuthenticationState>
    : RemoteAuthenticationResult<TRemoteAuthenticationState>
    where TRemoteAuthenticationState : RemoteAuthenticationState
{
    public string? Url { get; set; }
}