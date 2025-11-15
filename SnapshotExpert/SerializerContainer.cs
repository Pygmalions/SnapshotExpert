using System.Reflection;
using InjectionExpert;
using InjectionExpert.Utilities;
using Microsoft.Extensions.Logging;
using SnapshotExpert.Generator;
using SnapshotExpert.Serializers;

namespace SnapshotExpert;

public class SerializerContainer : ISerializerContainer
{
    /// <summary>
    /// Chained provider of all sources in this container.
    /// </summary>
    private readonly IInjectionProvider _injector;

    /// <summary>
    /// Container of registered by-type serializers.
    /// </summary>
    private readonly InjectionContainer _container = new();

    /// <summary>
    /// Provider of serializers provided by factories or the generator.
    /// </summary>
    private readonly InjectionFunctorProvider _resolver;

    /// <summary>
    /// Indicate whether to use the serializer generator or not.
    /// Configured from the constructor.
    /// </summary>
    private readonly bool _enableGenerator;

    /// <summary>
    /// Indicate whether to use serializer redirector or not.
    /// Configured from the constructor.
    /// </summary>
    private readonly bool _enableRedirector;

    /// <summary>
    /// Injections for instantiating serializers.
    /// </summary>
    public IInjectionProvider? Injections { get; init; }

    /// <summary>
    /// Logger for this container to use.
    /// </summary>
    public ILogger<SerializerContainer>? Logger { protected get; init; }

    public IList<ISerializerContainer.FactoryDelegate> Factories { get; } =
        new List<ISerializerContainer.FactoryDelegate>();

    /// <summary>
    /// Instantiate a serializer container.
    /// </summary>
    /// <param name="enableBuiltin">
    /// If true, this constructor will add built-in serializers during the instantiation.
    /// </param>
    /// <param name="enableGenerator">
    /// If true, <see cref="SerializerGenerator"/> will be used if no serializer is matched for the target type.
    /// </param>
    /// <param name="enableRedirector">
    /// If true, <see cref="SerializerRedirector{TTarget}"/> will be matched if the target type is an interface.
    /// </param>
    public SerializerContainer(
        bool enableBuiltin = true,
        bool enableRedirector = true,
        bool enableGenerator = true)
    {
        _resolver = IInjectionProvider.FromFunctor(GenerateSerializer, true);
        _injector = IInjectionProvider.FromMultiple(
            _container,
            _resolver,
            Injections!
        );
        _container.AddSingleton(this);

        _enableGenerator = enableGenerator;
        _enableRedirector = enableRedirector;

        if (enableBuiltin)
        {
            this.UsePrimitiveSerializers()
                .UseContainerSerializers();
        }
    }

    /// <summary>
    /// Generate a snapshot serializer for the specified target type
    /// and register it as a singleton in the container.
    /// </summary>
    /// <param name="serializerType">Serializer type to request.</param>
    /// <param name="key">Optional key to register with the serializer.</param>
    /// <param name="target">Injection target.</param>
    /// <returns>Item containing the generated serializer.</returns>
    private InjectionItem? GenerateSerializer(Type serializerType, object? key, InjectionTarget target)
    {
        if (!serializerType.IsGenericType ||
            serializerType.GetGenericTypeDefinition() != typeof(SnapshotSerializer<>))
            return null;

        var targetType = serializerType.GetGenericArguments()[0];

        // Search for the designated serializer.
        if (targetType.GetCustomAttribute<UsingSnapshotSerializerAttribute>() is
            { } designation)
        {
            return InjectionItem.Singleton(_injector.NewObject(designation.SerializerType));
        }

        // Search in factories.
        foreach (var factory in Factories)
        {
            if (factory(targetType, _injector) is { } factorySerializer)
                return InjectionItem.Singleton(factorySerializer);
        }

        // Generate a redirector if it is an interface.
        if (_enableRedirector && targetType.IsInterface)
        {
            /*
             * Generate redirector for interface types that cannot be handled by built-in extensions.
             * Note:
             * Abstract classes are not selected here because abstract classes are allowed to have
             * fields and properties with implementations.
             * When a serializer for an abstract class is requested,
             * the user may want the generator to handle the serialization of this abstract class as the
             * base type of another derived concrete class.
             */
            var redirector = (SnapshotSerializer)Activator.CreateInstance(
                typeof(SerializerRedirector<>).MakeGenericType(targetType))!;
            return InjectionItem.Singleton(redirector);
        }

        if (_enableGenerator)
        {
            // Use the generator to generate the serializer.
            return InjectionItem.Singleton(
                _injector.NewObject(SerializerGenerator.For(targetType)));
        }

        return null;
    }

    /// <summary>
    /// Get a snapshot serializer for the specified target type.
    /// </summary>
    /// <param name="targetType">Type of instances for the serializer to handle.</param>
    /// <returns>Serializer for the specified type, or null if not found.</returns>
    public SnapshotSerializer? GetSerializer(Type targetType)
    {
        return (SnapshotSerializer?)_injector.GetInjection(
            typeof(SnapshotSerializer<>).MakeGenericType(targetType));
    }

    public void AddSerializer(Type targetType, SnapshotSerializer serializer)
    {
        var categoryType = typeof(SnapshotSerializer<>).MakeGenericType(targetType);
        var serializerType = serializer.GetType();
        if (!categoryType.IsInstanceOfType(serializer))
            throw new ArgumentException(
                $"Cannot register serializer: serializer type is not assignable to '{categoryType}'.",
                nameof(serializer));
        _container.AddSingleton(categoryType, serializerType);
    }

    public void AddSerializer(Type targetType, Type serializerType)
    {
        var categoryType = typeof(SnapshotSerializer<>).MakeGenericType(targetType);
        if (!serializerType.IsAssignableTo(categoryType))
            throw new ArgumentException(
                $"Cannot register serializer: serializer type is not assignable to '{categoryType}'.",
                nameof(serializerType));
        _container.AddSingleton(categoryType, serializerType);
    }

    public void RemoveSerializer(Type targetType)
    {
        _container.RemoveInjection(
            typeof(SnapshotSerializer<>).MakeGenericType(targetType), null);
    }

    public void Clear()
    {
        _container.Clear();
    }

    public void InvalidateCache()
    {
        _container.InvalidateCache();
        _resolver.InvalidateCache();
    }
}