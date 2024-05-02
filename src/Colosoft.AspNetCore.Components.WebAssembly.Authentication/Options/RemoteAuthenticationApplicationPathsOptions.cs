namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticationApplicationPathsOptions
{
    public string RegisterPath { get; set; } = RemoteAuthenticationDefaults.RegisterPath;

    public string? RemoteRegisterPath { get; set; }

    public string ProfilePath { get; set; } = RemoteAuthenticationDefaults.ProfilePath;

    public string? RemoteProfilePath { get; set; }

    public string LogInPath { get; set; } = RemoteAuthenticationDefaults.LoginPath;

    public string LogInCallbackPath { get; set; } = RemoteAuthenticationDefaults.LoginCallbackPath;

    public string LogInFailedPath { get; set; } = RemoteAuthenticationDefaults.LoginFailedPath;

    public string LogOutPath { get; set; } = RemoteAuthenticationDefaults.LogoutPath;

    public string LogOutCallbackPath { get; set; } = RemoteAuthenticationDefaults.LogoutCallbackPath;

    public string LogOutFailedPath { get; set; } = RemoteAuthenticationDefaults.LogoutFailedPath;

    public string LogOutSucceededPath { get; set; } = RemoteAuthenticationDefaults.LogoutSucceededPath;
}
