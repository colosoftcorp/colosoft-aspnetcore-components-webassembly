namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticationResult<TRemoteAuthenticationState>
    where TRemoteAuthenticationState : RemoteAuthenticationState
{
    public RemoteAuthenticationStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public TRemoteAuthenticationState? State { get; set; }
}
