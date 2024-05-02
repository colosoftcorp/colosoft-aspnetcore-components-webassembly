namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public static class RemoteAuthenticationActions
{
    public const string LogIn = "login";

    public const string LogInCallback = "login-callback";

    public const string LogInFailed = "login-failed";

    public const string Profile = "profile";

    public const string Register = "register";

    public const string LogOut = "logout";

    public const string LogOutCallback = "logout-callback";

    public const string LogOutFailed = "logout-failed";

    public const string LogOutSucceeded = "logged-out";

    public static bool IsAction(string action, string candidate) => action != null && string.Equals(action, candidate, System.StringComparison.OrdinalIgnoreCase);
}
