namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class HasValidAccessTokenRequestOptions
{
    public IEnumerable<string>? Scopes { get; set; }

    public bool ValidateAuthenticationServerConnection { get; set; }
}
