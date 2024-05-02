using System.Diagnostics.CodeAnalysis;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class AccessTokenResult
{
    private readonly AccessToken? token;

    [Obsolete("Use the AccessTokenResult(AccessTokenResultStatus, AccessToken, string, InteractiveRequestOptions)")]
    public AccessTokenResult(
        AccessTokenResultStatus status,
        AccessToken? token,
        [StringSyntax(StringSyntaxAttribute.Uri)] string redirectUrl)
    {
        this.Status = status;
        this.token = token;
        this.RedirectUrl = redirectUrl;
    }

    public AccessTokenResult(
        AccessTokenResultStatus status,
        AccessToken? token,
        [StringSyntax(StringSyntaxAttribute.Uri)] string? interactiveRequestUrl,
        InteractiveRequestOptions? interactiveRequest)
    {
        this.Status = status;
        this.token = token;
        this.InteractiveRequestUrl = interactiveRequestUrl;
        this.InteractionOptions = interactiveRequest;
    }

    public AccessTokenResultStatus Status { get; }

    [Obsolete("Use 'InteractiveRequestUrl' and 'InteractiveRequest' instead.")]
    public string? RedirectUrl { get; }

    public string? InteractiveRequestUrl { get; }

    public InteractiveRequestOptions? InteractionOptions { get; }

    public bool TryGetToken([NotNullWhen(true)] out AccessToken? accessToken)
    {
        if (this.Status == AccessTokenResultStatus.Success)
        {
            accessToken = this.token;
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
            return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
        }
        else
        {
            accessToken = null;
            return false;
        }
    }
}
