using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

[JsonConverter(typeof(Converter))]
public sealed class InteractiveRequestOptions
{
    required public InteractionType Interaction { get; init; }

    required public string ReturnUrl { get; init; }

    public IEnumerable<string> Scopes { get; init; } = Array.Empty<string>();

    private Dictionary<string, object>? AdditionalRequestParameters { get; set; }

    internal static InteractiveRequestOptions FromState(string state) => JsonSerializer.Deserialize(
        state,
        InteractiveRequestOptionsSerializerContext.Default.InteractiveRequestOptions) !;

    public bool TryAddAdditionalParameter<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string name, TValue value)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        this.AdditionalRequestParameters ??= new();
        return this.AdditionalRequestParameters.TryAdd(name, value!);
    }

    public bool TryRemoveAdditionalParameter(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return this.AdditionalRequestParameters != null && this.AdditionalRequestParameters.Remove(name);
    }

    public bool TryGetAdditionalParameter<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string name, [NotNullWhen(true)] out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(name);

        value = default;
        if (this.AdditionalRequestParameters == null || !this.AdditionalRequestParameters.TryGetValue(name, out var rawValue))
        {
            return false;
        }

        if (rawValue is JsonElement json)
        {
            value = Deserialize(json) !;
            this.AdditionalRequestParameters[name] = value;
            return true;
        }
        else
        {
            value = (TValue)rawValue;
            return true;
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "The types this method deserializes are anotated with 'DynamicallyAccessedMembers' to prevent them from being linked out as part of 'TryAddAdditionalParameter'.")]
        static TValue Deserialize(JsonElement element) => element.Deserialize<TValue>() !;
    }

    internal string ToState() => JsonSerializer.Serialize(this, InteractiveRequestOptionsSerializerContext.Default.InteractiveRequestOptions);

    internal class Converter : JsonConverter<InteractiveRequestOptions>
    {
        public override InteractiveRequestOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var requestOptions = JsonSerializer.Deserialize(ref reader, InteractiveRequestOptionsSerializerContext.Default.OptionsRecord);

            return new InteractiveRequestOptions
            {
                AdditionalRequestParameters = requestOptions.AdditionalRequestParameters,
                Interaction = requestOptions.Interaction,
                ReturnUrl = requestOptions.ReturnUrl,
                Scopes = requestOptions.Scopes,
            };
        }

        public override void Write(Utf8JsonWriter writer, InteractiveRequestOptions value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(
                writer,
                new OptionsRecord(value.ReturnUrl, value.Scopes, value.Interaction, value.AdditionalRequestParameters),
                InteractiveRequestOptionsSerializerContext.Default.OptionsRecord);
        }

        internal readonly struct OptionsRecord
        {
            [JsonInclude]
            public string ReturnUrl { get; init; }

            [JsonInclude]
            public IEnumerable<string> Scopes { get; init; }

            [JsonInclude]
            public InteractionType Interaction { get; init; }

            [JsonInclude]
            public Dictionary<string, object>? AdditionalRequestParameters { get; init; }

            public OptionsRecord(
                string returnUrl,
                IEnumerable<string> scopes,
                InteractionType interaction,
                Dictionary<string, object>? additionalRequestParameters)
            {
                this.ReturnUrl = returnUrl;
                this.Scopes = scopes;
                this.Interaction = interaction;
                this.AdditionalRequestParameters = additionalRequestParameters;
            }
        }
    }
}
