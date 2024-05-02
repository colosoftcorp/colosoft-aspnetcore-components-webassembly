namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticatorView : RemoteAuthenticatorViewCore<RemoteAuthenticationState>
{
    public RemoteAuthenticatorView() => this.AuthenticationState = new RemoteAuthenticationState();
}
