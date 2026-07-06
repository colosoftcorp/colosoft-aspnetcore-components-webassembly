using System.Text.Json.Serialization;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class OidcClientOptions
    {
        public string? Authority { get; set; }

        public string? MetadataUrl { get; set; }

        [JsonPropertyName("client_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClientSecret { get; set; }

        [JsonPropertyName("response_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResponseType { get; set; }

        [JsonConverter(typeof(Json.SpaceSeparatedStringListConverterFactory))]
        public IList<string> Scope { get; } = new List<string> { "openid", "profile" };

        [JsonPropertyName("redirect_uri")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RedirectUri { get; set; }

        [JsonPropertyName("post_logout_redirect_uri")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PostLogoutRedirectUri { get; set; }

        [JsonPropertyName("client_authentication")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClientAuthentication { get; set; }

        [JsonPropertyName("token_endpoint_auth_signing_alg")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TokenEndpointAuthSigningAlg { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Prompt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Display { get; set; }

        [JsonPropertyName("max_age")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxAge { get; set; }

        [JsonPropertyName("ui_locales")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UiLocales { get; set; }

        [JsonPropertyName("acr_values")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AcrValues { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        public string[]? Resource { get; set; }
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

        [JsonPropertyName("response_mode")]
        public string? ResponseMode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        public string[]? FilterProtocolClaims { get; set; }
#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? LoadUserInfo { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? StaleStateAgeInSeconds { get; set; }

        [JsonPropertyName("extraQueryParams")]
        public IDictionary<string, string> AdditionalProviderParameters { get; } = new Dictionary<string, string>();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string>? ExtraTokenParams { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, object>? ExtraHeaders { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RevokeTokenAdditionalContentTypes { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? DisablePKCE { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FetchRequestCredentials { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefreshTokenAllowedScope { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RequestTimeoutInSeconds { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? OmitScopeWhenRequesting { get; set; }
    }
}
