namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public interface IAccessTokenProvider
{
    ValueTask<AccessTokenResult> RequestAccessToken();

    ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options);
}
