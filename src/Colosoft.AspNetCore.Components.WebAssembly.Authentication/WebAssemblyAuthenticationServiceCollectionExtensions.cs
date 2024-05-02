using Colosoft.AspNetCore.Components.WebAssembly.Authentication;
using Colosoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.Extensions.DependencyInjection;

public static class WebAssemblyAuthenticationServiceCollectionExtensions
{
#pragma warning disable S4136 // Method overloads should be grouped together
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddRemoteAuthentication<
#pragma warning restore S4136 // Method overloads should be grouped together
        [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState,
        [DynamicallyAccessedMembers(JsonSerialized)] TAccount,
        [DynamicallyAccessedMembers(JsonSerialized)] TProviderOptions>(
        this IServiceCollection services)
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TAccount : RemoteUserAccount
        where TProviderOptions : class, new()
    {
        services.AddOptions();
        services.AddAuthorizationCore();
        services.TryAddScoped<AuthenticationStateProvider, RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>>();
        AddAuthenticationStateProvider<TRemoteAuthenticationState>(services);

        services.TryAddTransient<BaseAddressAuthorizationMessageHandler>();
        services.TryAddTransient<AuthorizationMessageHandler>();

        services.TryAddScoped(sp =>
        {
            return (IAccessTokenProvider)sp.GetRequiredService<AuthenticationStateProvider>();
        });

        services.TryAddScoped<IRemoteAuthenticationPathsProvider, DefaultRemoteApplicationPathsProvider<TProviderOptions>>();
        services.TryAddScoped<IAccessTokenProviderAccessor, AccessTokenProviderAccessor>();
#pragma warning disable CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility
        services.TryAddScoped<SignOutSessionStateManager>();
#pragma warning restore CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility

        services.TryAddScoped<AccountClaimsPrincipalFactory<TAccount>>();

        return new RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>(services);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091", Justification = "The calling method enforces the dynamically accessed members constraints.")]
    private static void AddAuthenticationStateProvider<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(IServiceCollection services)
        where TRemoteAuthenticationState : RemoteAuthenticationState
    {
        services.TryAddScoped(static sp => (IRemoteAuthenticationService<TRemoteAuthenticationState>)sp.GetRequiredService<AuthenticationStateProvider>());
    }

    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddRemoteAuthentication<
        [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount, [DynamicallyAccessedMembers(JsonSerialized)] TProviderOptions>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<TProviderOptions>>? configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TAccount : RemoteUserAccount
        where TProviderOptions : class, new()
    {
        services.AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, TProviderOptions>();
        if (configure != null)
        {
            services.Configure(configure);
        }

        return new RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>(services);
    }

    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddOidcAuthentication(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
    {
        return AddOidcAuthentication<RemoteAuthenticationState>(services, configure);
    }

    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddOidcAuthentication<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    {
        return AddOidcAuthentication<TRemoteAuthenticationState, RemoteUserAccount>(services, configure);
    }

    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddOidcAuthentication<
        [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPostConfigureOptions<RemoteAuthenticationOptions<OidcProviderOptions>>, DefaultOidcOptionsConfiguration>());

        return AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, OidcProviderOptions>(services, configure);
    }

    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization(this IServiceCollection services)
    {
        return AddApiAuthorizationCore<RemoteAuthenticationState, RemoteUserAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name!);
    }

    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(this IServiceCollection services)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    {
        return AddApiAuthorizationCore<TRemoteAuthenticationState, RemoteUserAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name!);
    }

    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiAuthorization<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        this IServiceCollection services)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
    {
        return AddApiAuthorizationCore<TRemoteAuthenticationState, TAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name!);
    }

    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
    {
        return AddApiAuthorizationCore<RemoteAuthenticationState, RemoteUserAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name!);
    }

    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    {
        return AddApiAuthorizationCore<TRemoteAuthenticationState, RemoteUserAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name!);
    }

    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiAuthorization<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
    {
        return AddApiAuthorizationCore<TRemoteAuthenticationState, TAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name!);
    }

    private static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiAuthorizationCore<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        IServiceCollection services,
        Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>? configure,
        string inferredClientId)
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TAccount : RemoteUserAccount
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IPostConfigureOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>, DefaultApiAuthorizationOptionsConfiguration>(_ =>
            new DefaultApiAuthorizationOptionsConfiguration(inferredClientId)));

        services.AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, ApiAuthorizationProviderOptions>(configure);

        return new RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>(services);
    }
}
