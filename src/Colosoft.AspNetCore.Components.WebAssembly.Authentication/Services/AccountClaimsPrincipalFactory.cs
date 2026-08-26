using Colosoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System.Security.Claims;
using System.Text.Json;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class AccountClaimsPrincipalFactory<TAccount>
    where TAccount : RemoteUserAccount
{
    private readonly IAccessTokenProviderAccessor accessor;

    public AccountClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) => this.accessor = accessor;

    public IAccessTokenProvider TokenProvider => this.accessor.TokenProvider;

    public virtual ValueTask<ClaimsPrincipal> CreateUserAsync(
        TAccount account,
        RemoteAuthenticationUserOptions options)
    {
        var identity = account != null
            ? new ClaimsIdentity(
                options.AuthenticationType,
                options.NameClaim,
                options.RoleClaim)
            : new ClaimsIdentity();

        if (account != null)
        {
            foreach (var kvp in account.AdditionalProperties)
            {
                var name = kvp.Key;
                var value = kvp.Value;
                if (value != null ||
                    (value is JsonElement element &&
                     element.ValueKind != JsonValueKind.Undefined &&
                     element.ValueKind != JsonValueKind.Null))
                {
                    identity.AddClaim(new Claim(name, value.ToString() !));
                }
            }
        }

        return new ValueTask<ClaimsPrincipal>(new ClaimsPrincipal(identity));
    }
}
