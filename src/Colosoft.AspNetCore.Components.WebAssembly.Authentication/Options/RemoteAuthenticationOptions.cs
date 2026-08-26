using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Colosoft.AspNetCore.Components.WebAssembly.Authentication;

public class RemoteAuthenticationOptions<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationProviderOptions>
    where TRemoteAuthenticationProviderOptions : new()
{
    private static readonly MethodInfo GetDefaultValueMethodInfo = typeof(RemoteAuthenticationOptions<TRemoteAuthenticationProviderOptions>)
        .GetMethod(nameof(GetDefaultValueGeneric), BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"Method '{nameof(GetDefaultValueGeneric)}' was not found.");

    private readonly IList<ObserverTypeDescriptor> observerTypes = new List<ObserverTypeDescriptor>();
    private readonly IList<Func<IServiceProvider, IRemoteAuthenticationServiceObserver>> observerFactories = new List<Func<IServiceProvider, IRemoteAuthenticationServiceObserver>>();
    private readonly IList<IRemoteAuthenticationServiceObserver> observers = new List<IRemoteAuthenticationServiceObserver>();

    private static object? GetDefaultValueGeneric<T>()
    {
        return default(T);
    }

    public RemoteAuthenticationOptions()
        : this(null)
    {
    }

    public RemoteAuthenticationOptions(IServiceProvider? serviceProvider)
    {
    }

    public TRemoteAuthenticationProviderOptions ProviderOptions { get; } = new TRemoteAuthenticationProviderOptions();

    public RemoteAuthenticationApplicationPathsOptions AuthenticationPaths { get; } = new RemoteAuthenticationApplicationPathsOptions();

    public RemoteAuthenticationUserOptions UserOptions { get; } = new RemoteAuthenticationUserOptions();

    public void AddObserver(Func<IServiceProvider, IRemoteAuthenticationServiceObserver> observerFactory)
    {
        ArgumentNullException.ThrowIfNull(observerFactory);
        this.observerFactories.Add(observerFactory);
    }

    public void AddObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TObserver>()
        where TObserver : IRemoteAuthenticationServiceObserver
    {
        this.observerTypes.Add(new ObserverTypeDescriptor(typeof(TObserver)));
    }

    public void AddObserver(IRemoteAuthenticationServiceObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        this.observers.Add(observer);
    }

    public IEnumerable<IRemoteAuthenticationServiceObserver> GetObservers(
        IServiceProvider? serviceProvider)
    {
        foreach (var observerType in this.observerTypes)
        {
            var observerInstance = this.CreateObserverInstance(observerType.Type, serviceProvider);

            if (observerInstance is IRemoteAuthenticationServiceObserver observer)
            {
                yield return observer;
            }
        }

        if (serviceProvider != null)
        {
            foreach (var observerFactory in this.observerFactories)
            {
                yield return observerFactory(serviceProvider);
            }
        }

        foreach (var observer in this.observers)
        {
            yield return observer;
        }
    }

    private object? CreateObserverInstance(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type observerType,
        IServiceProvider? serviceProvider)
    {
        var observerInstance = serviceProvider?.GetService(observerType);

        if (observerInstance != null)
        {
            return observerInstance;
        }

        var constructor = observerType
            .GetConstructors()
            .OrderByDescending(i => i.GetParameters().Length)
            .FirstOrDefault();

        if (constructor == null)
        {
            return null;
        }

        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var value = serviceProvider?.GetService(parameter.ParameterType);

            if (value == null)
            {
                value = parameter.HasDefaultValue
                    ? parameter.DefaultValue
                    : this.GetDefaultValue(parameter.ParameterType);
            }

            arguments[i] = value;
        }

        return constructor.Invoke(arguments);
    }

    private object? GetDefaultValue(Type type)
    {
        if (!type.IsValueType)
        {
            return null;
        }

        return GetDefaultValueMethodInfo.MakeGenericMethod(type).Invoke(null, null);
    }

    private sealed class ObserverTypeDescriptor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type Type { get; } = type;
    }
}
