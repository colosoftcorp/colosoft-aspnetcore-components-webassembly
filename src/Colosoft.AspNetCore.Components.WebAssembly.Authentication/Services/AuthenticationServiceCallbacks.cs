using Microsoft.JSInterop;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

internal class AuthenticationServiceCallbacks
{
    public event Func<Task>? UserLoaded;
    public event Func<Task>? UserUnloaded;
    public event Func<Task>? AccessTokenExpiring;
    public event Func<Task>? AccessTokenExpired;
    public event Func<string, Task>? SilentRenewError;
    public event Func<Task>? UserSignOut;
    public event Func<Task>? UserSessionChanged;

    [JSInvokable("onUserLoaded")]
    public async ValueTask OnUserLoaded()
    {
        if (this.UserLoaded != null)
        {
            await this.UserLoaded();
        }
    }

    [JSInvokable("onUserUnloaded")]
    public async ValueTask OnUserUnloaded()
    {
        if (this.UserUnloaded != null)
        {
            await this.UserUnloaded();
        }
    }

    [JSInvokable("onAccessTokenExpiring")]
    public async ValueTask OnAccessTokenExpiring()
    {
        if (this.AccessTokenExpiring != null)
        {
            await this.AccessTokenExpiring();
        }
    }

    [JSInvokable("onAccessTokenExpired")]
    public async ValueTask OnAccessTokenExpired()
    {
        if (this.AccessTokenExpired != null)
        {
            await this.AccessTokenExpired();
        }
    }

    [JSInvokable("onSilentRenewError")]
    public async ValueTask OnSilentRenewError(string error)
    {
        if (this.SilentRenewError != null)
        {
            await this.SilentRenewError(error);
        }
    }

    [JSInvokable("onUserSignOut")]
    public async ValueTask OnUserSignOut()
    {
        if (this.UserSignOut != null)
        {
            await this.UserSignOut();
        }
    }

    [JSInvokable("onUserSessionChanged")]
    public async ValueTask OnUserSessionChanged()
    {
        if (this.UserSessionChanged != null)
        {
            await this.UserSessionChanged();
        }
    }
}
