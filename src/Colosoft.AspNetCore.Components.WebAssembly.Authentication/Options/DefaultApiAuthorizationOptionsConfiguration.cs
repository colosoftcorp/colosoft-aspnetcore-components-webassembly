using Microsoft.Extensions.Options;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DefaultApiAuthorizationOptionsConfiguration : IPostConfigureOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>
{
    private readonly string applicationName;

    public DefaultApiAuthorizationOptionsConfiguration(string applicationName) => this.applicationName = applicationName;

    public void Configure(RemoteAuthenticationOptions<ApiAuthorizationProviderOptions> options)
    {
        options.ProviderOptions.ConfigurationEndpoint ??= $"_configuration/{this.applicationName}";
        options.AuthenticationPaths.RemoteRegisterPath ??= "Identity/Account/Register";
        options.AuthenticationPaths.RemoteProfilePath ??= "Identity/Account/Manage";
        options.UserOptions.ScopeClaim ??= "scope";
        options.UserOptions.RoleClaim ??= "role";
        options.UserOptions.AuthenticationType ??= this.applicationName;
    }

    public void PostConfigure(string? name, RemoteAuthenticationOptions<ApiAuthorizationProviderOptions> options)
    {
        if (string.Equals(name, Options.DefaultName))
        {
            this.Configure(options);
        }
    }
}
