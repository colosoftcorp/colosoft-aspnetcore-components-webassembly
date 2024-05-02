using Colosoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection;

public static class RemoteAuthenticationBuilderExtensions
{
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddAccountClaimsPrincipalFactory<TRemoteAuthenticationState, TAccount, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccountClaimsPrincipalFactory>(
        this IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> builder)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
        where TAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<TAccount>
    {
        builder.Services.Replace(ServiceDescriptor.Scoped<AccountClaimsPrincipalFactory<TAccount>, TAccountClaimsPrincipalFactory>());

        return builder;
    }

    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddAccountClaimsPrincipalFactory<TRemoteAuthenticationState, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccountClaimsPrincipalFactory>(
        this IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> builder)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount> => builder.AddAccountClaimsPrincipalFactory<TRemoteAuthenticationState, RemoteUserAccount, TAccountClaimsPrincipalFactory>();

    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddAccountClaimsPrincipalFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccountClaimsPrincipalFactory>(
        this IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> builder)
        where TAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount> => builder.AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, RemoteUserAccount, TAccountClaimsPrincipalFactory>();
}
