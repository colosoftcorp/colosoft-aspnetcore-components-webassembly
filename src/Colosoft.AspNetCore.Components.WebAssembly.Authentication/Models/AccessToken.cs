namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class AccessToken
{
    public IReadOnlyList<string> GrantedScopes { get; set; } = default!;

    public DateTimeOffset Expires { get; set; }

    public string Value { get; set; } = default!;
}
