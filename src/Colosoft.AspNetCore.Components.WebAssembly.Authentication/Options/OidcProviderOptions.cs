using System.Text.Json.Serialization;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class OidcProviderOptions
{
    public string? Authority { get; set; }

    public string? MetadataUrl { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    public IList<string> DefaultScopes { get; } = new List<string> { "openid", "profile" };

    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }

    [JsonPropertyName("post_logout_redirect_uri")]
    public string? PostLogoutRedirectUri { get; set; }

    [JsonPropertyName("response_type")]
    public string? ResponseType { get; set; }

    [JsonPropertyName("response_mode")]
    public string? ResponseMode { get; set; }

    [JsonPropertyName("extraQueryParams")]
    public IDictionary<string, string> AdditionalProviderParameters { get; } = new Dictionary<string, string>();
}
