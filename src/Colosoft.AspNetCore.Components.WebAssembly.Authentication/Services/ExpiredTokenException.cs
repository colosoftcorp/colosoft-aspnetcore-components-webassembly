using Microsoft.AspNetCore.Components;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class AccessTokenNotAvailableException : Exception
{
    private readonly NavigationManager navigation;
    private readonly AccessTokenResult tokenResult;

    public AccessTokenNotAvailableException(
        NavigationManager navigation,
        AccessTokenResult tokenResult,
        IEnumerable<string>? scopes)
        : base(
#pragma warning disable S2583 // Conditionally executed code should be reachable
            message: "Unable to provision an access token for the requested scopes: " +
              scopes != null ? $"'{string.Join(", ", scopes ?? Array.Empty<string>())}'" : "(default scopes)")
#pragma warning restore S2583 // Conditionally executed code should be reachable
    {
        this.tokenResult = tokenResult;
        this.navigation = navigation;
    }

    public void Redirect()
    {
        if (this.tokenResult.InteractionOptions != null && this.tokenResult.InteractiveRequestUrl != null)
        {
            this.navigation.NavigateToLogin(this.tokenResult.InteractiveRequestUrl, this.tokenResult.InteractionOptions);
        }
        else
        {
#pragma warning disable CS0618 // Type or member is obsolete
            this.navigation.NavigateTo(this.tokenResult.RedirectUrl!);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    public void Redirect(Action<InteractiveRequestOptions> configureInteractionOptions)
    {
        ArgumentNullException.ThrowIfNull(configureInteractionOptions);
        configureInteractionOptions(this.tokenResult.InteractionOptions!);
        this.navigation.NavigateToLogin(this.tokenResult.InteractiveRequestUrl!, this.tokenResult.InteractionOptions!);
    }
}
