namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticationUserOptions
{
    public string NameClaim { get; set; } = "name";

    public string? RoleClaim { get; set; }

    public string? ScopeClaim { get; set; }

    public string? AuthenticationType { get; set; }
}
