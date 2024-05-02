namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication.Internal;

public interface IAccessTokenProviderAccessor
{
    IAccessTokenProvider TokenProvider { get; }
}
