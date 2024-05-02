namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class AccessTokenRequestOptions
{
    public IEnumerable<string>? Scopes { get; set; }

    public string? ReturnUrl { get; set; }
}
