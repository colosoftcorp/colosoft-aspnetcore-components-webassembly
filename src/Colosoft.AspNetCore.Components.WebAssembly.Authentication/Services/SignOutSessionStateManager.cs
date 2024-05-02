using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

[Obsolete("Use 'Colosoft.AspNetCore.Components.WebAssembly.Authentication.NavigationManagerExtensions.NavigateToLogout' instead.")]
public class SignOutSessionStateManager
{
    private static readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly IJSRuntime jsRuntime;

    public SignOutSessionStateManager(IJSRuntime jsRuntime) => this.jsRuntime = jsRuntime;

    [DynamicDependency(JsonSerialized, typeof(SignOutState))]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The correct members will be preserved by the above DynamicDependency.")]
    private static SignOutState DeserializeSignOutState(string result) => JsonSerializer.Deserialize<SignOutState>(result, SerializationOptions);

    [DynamicDependency(JsonSerialized, typeof(SignOutState))]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The correct members will be preserved by the above DynamicDependency.")]
    public virtual ValueTask SetSignOutState()
    {
        return this.jsRuntime.InvokeVoidAsync(
            "sessionStorage.setItem",
            "Colosoft.AspNetCore.Components.WebAssembly.Authentication.SignOutState",
            JsonSerializer.Serialize(SignOutState.Instance, SerializationOptions));
    }

    public virtual async Task<bool> ValidateSignOutState()
    {
        var state = await this.GetSignOutState();
        if (state.Local)
        {
            await this.ClearSignOutState();
            return true;
        }

        return false;
    }

    private async ValueTask<SignOutState> GetSignOutState()
    {
        var result = await this.jsRuntime.InvokeAsync<string>(
            "sessionStorage.getItem",
            "Colosoft.AspNetCore.Components.WebAssembly.Authentication.SignOutState");
        if (result == null)
        {
            return default;
        }

        return DeserializeSignOutState(result);
    }

    private ValueTask ClearSignOutState()
    {
        return this.jsRuntime.InvokeVoidAsync(
            "sessionStorage.removeItem",
            "Colosoft.AspNetCore.Components.WebAssembly.Authentication.SignOutState");
    }

    private struct SignOutState
    {
        public static readonly SignOutState Instance = new SignOutState { Local = true };

        public bool Local { get; set; }
    }
}
