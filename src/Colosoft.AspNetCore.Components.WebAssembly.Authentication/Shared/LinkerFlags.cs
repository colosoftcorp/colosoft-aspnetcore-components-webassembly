using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Internal;

internal static class LinkerFlags
{
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

    public const DynamicallyAccessedMemberTypes Component = DynamicallyAccessedMemberTypes.All;

    public const DynamicallyAccessedMemberTypes JSInvokable = DynamicallyAccessedMemberTypes.PublicMethods;
}
