using Colosoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System.Security.Claims;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal class TestAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<CoolRoleAccount>
{
    public TestAccountClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor)
        : base(accessor)
    {
    }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        CoolRoleAccount account,
        RemoteAuthenticationUserOptions options)
    {
        var user = await base.CreateUserAsync(account, options);

        if (account.CoolRole != null)
        {
            foreach (var role in account.CoolRole)
            {
                ((ClaimsIdentity)user.Identity!).AddClaim(new Claim("CoolRole", role));
            }
        }

        return user;
    }
}
