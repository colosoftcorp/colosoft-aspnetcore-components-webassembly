using Microsoft.AspNetCore.Components;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class BaseAddressAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public BaseAddressAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigationManager)
        : base(provider, navigationManager)
    {
        this.ConfigureHandler(new[] { navigationManager.BaseUri });
    }
}
