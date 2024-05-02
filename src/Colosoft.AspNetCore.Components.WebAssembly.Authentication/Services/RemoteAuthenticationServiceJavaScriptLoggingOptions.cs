namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

// We need to do this as it can't be nested inside RemoteAuthenticationService because
// it needs to be put in an attribute for linking purposes and that can't be an open generic type.
internal class RemoteAuthenticationServiceJavaScriptLoggingOptions
{
    public bool DebugEnabled { get; set; }

    public bool TraceEnabled { get; set; }
}
