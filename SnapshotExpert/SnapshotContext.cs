using System.Reflection;
using DocumentationParser;
using InjectionExpert;
using Microsoft.Extensions.Logging;
using SnapshotExpert.Generator;
using SnapshotExpert.Serializers;
using SnapshotExpert.Utilities;

namespace SnapshotExpert;

public class SnapshotContext
{
    private readonly InjectionContainer _container = new();

    private readonly IInjectionProvider _injector;

    /// <summary>
    /// Injections to use when instantiate serializers.
    /// </summary>
    public IInjectionProvider? Injections { get; init; }
    
    /// <summary>
    /// Logger factory to inject loggers for serializers.
    /// </summary>
    public ILoggerFactory? LoggerFactory
    {
        get;
        init
        {
            field = value;
            if (value == null)
                return;
            _container.AddSingleton(value);
            _container.AddInjection(
                typeof(ILogger<>),
                (_, type, _) => Activator.CreateInstance(
                    typeof(Logger<>).MakeGenericType(type.GetGenericArguments()[0]), value)!,
                InjectionLifespan.Transient);
        }
    }

    public IDocumentationProvider? Documentation
    {
        get;
        init
        {
            field = value;
            if (value == null)
                return;
            _container.AddSingleton(value);
        }
    }

    public SnapshotContext()
    {
        _injector = IInjectionProvider.FromMultiple(
            // Embedded serializers and injections.
            _container,
            // Generate serializers according to special rules.
            IInjectionProvider.FromFunctor(GenerateSerializer, false),
            // User-provided injections.
            Injections);
        _container
            .AddSingleton(this)
            .WithPrimitiveSerializers();
    }

    /// <summary>
    /// Generate a snapshot serializer for the specified target type,
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

        if (targetType.GetCustomAttribute<UsingSnapshotSerializerAttribute>() is
            { } designation)
            serializerType = designation.SerializerType;
        else if (SnapshotContextExtensionForGenerics.GetSerializerType(targetType)
                 is { } genericSerializerType)
            serializerType = genericSerializerType;
        else if (SnapshotContextExtensionForContainers.GetSerializerType(targetType)
                 is { } containerSerializerType)
            serializerType = containerSerializerType;
        else if (targetType.IsInterface)
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
                typeof(SnapshotSerializerRedirector<>).MakeGenericType(targetType))!;
            _container.AddInjection(serializerType, redirector);
            return new InjectionItem(redirector, InjectionLifespan.Singleton);
        }
        else
        {
            // Use the generator to generate the serializer.
            serializerType = SerializerGenerator.For(targetType);
        }
        
        var serializer = _injector.NewObject(serializerType);
        _container.AddInjection(serializerType, serializerType);
        return new InjectionItem(serializer, InjectionLifespan.Singleton);
    }

    /// <summary>
    /// Get a snapshot serializer for the specified target type.
    /// </summary>
    /// <param name="targetType">Type of instances for the serializer to handle.</param>
    /// <returns>Serializer for the specified type, or null if not found.</returns>
    public SnapshotSerializer? GetSerializer(Type targetType)
    {
        var serializer = (SnapshotSerializer?)_injector.GetInjection(
            typeof(SnapshotSerializer<>).MakeGenericType(targetType));
        return serializer;
    }
    
    /// <summary>
    /// Register a serializer instance for the specified target type.
    /// </summary>
    /// <param name="targetType">Type for this serializer instance to handle.</param>
    /// <param name="serializer">Serializer instance to register.</param>
    /// <returns>This snapshot context.</returns>
    /// <exception cref="ArgumentException">
    /// Throw if the serializer instance is not assignable to the corresponding serializer type.
    /// </exception>
    internal SnapshotContext UseSerializer(Type targetType, SnapshotSerializer serializer)
    {
        var serializerType = serializer.GetType();
        var categoryType = typeof(SnapshotSerializer<>).MakeGenericType(targetType);
        if (!serializerType.IsAssignableTo(categoryType))
            throw new ArgumentException(
                $"Cannot register serializer: serializer instance is not assignable to '{categoryType}'.", 
                nameof(serializer));
        _container.AddInjection(serializerType, serializer);
        _container.AddRedirection(categoryType, null, serializerType, null);
        return this;
    }
    
    /// <summary>
    /// Register a serializer type for the specified target type.
    /// </summary>
    /// <param name="targetType">Type for the serializer to handle.</param>
    /// <param name="serializerType">Serializer type to register.</param>
    /// <returns>This snapshot context.</returns>
    /// <exception cref="ArgumentException">
    /// Throw if the serializer type is not assignable to the corresponding serializer type.
    /// </exception>
    internal SnapshotContext UseSerializer(Type targetType, Type serializerType)
    {
        var categoryType = typeof(SnapshotSerializer<>).MakeGenericType(targetType);
        if (!serializerType.IsAssignableTo(categoryType))
            throw new ArgumentException(
                $"Cannot register serializer: serializer type is not assignable to '{categoryType}'.", 
                nameof(serializerType));
        _container.AddSingleton(serializerType, serializerType);
        _container.AddRedirection(categoryType, null, serializerType, null);
        return this;
    }
    
    /// <summary>
    /// Register a serializer type for the specified target type.
    /// </summary>
    /// <typeparam name="TTarget">Type for the serializer to handle.</typeparam>
    /// <typeparam name="TSerializer">Serializer type to register.</typeparam>
    /// <returns>This snapshot context.</returns>
    /// <exception cref="ArgumentException">
    /// Throw if the serializer type is not assignable to the corresponding serializer type.
    /// </exception>
    internal SnapshotContext UseSerializer<TTarget, TSerializer>()
        where TSerializer : SnapshotSerializer<TTarget>
        => UseSerializer(typeof(TTarget), typeof(TSerializer));
}

public static class SnapshotContextExtensions
{
    /// <summary>
    /// Get a snapshot serializer for the specified target type.
    /// </summary>
    /// <typeparam name="TTarget">Type of instances for the serializer to handle.</typeparam>
    /// <param name="context">Snapshot context to get serializer from.</param>
    /// <returns>Serializer for the specified type, or null if not found.</returns>
    public static SnapshotSerializer<TTarget>? GetSerializer<TTarget>(this SnapshotContext context)
        => context.GetSerializer(typeof(TTarget)) as SnapshotSerializer<TTarget>;

    /// <summary>
    /// Get a snapshot serializer for the specified target type,
    /// or throw an exception if it is not found.
    /// </summary>
    /// <param name="type">Type of instances for the serializer to handle.</param>
    /// <param name="context">Snapshot context to get serializer from.</param>
    /// <returns>Serializer for the specified type.</returns>
    /// <exception cref="Exception">
    /// Throw if the serializer for the specified type is not found.
    /// </exception>
    public static SnapshotSerializer RequireSerializer(this SnapshotContext context, Type type)
        => context.GetSerializer(type)
           ?? throw new Exception($"Failed to find the required serializer for '{type}'.");

    /// <summary>
    /// Get a snapshot serializer for the specified target type,
    /// or throw an exception if it is not found.
    /// </summary>
    /// <typeparam name="TTarget">Type of instances for the serializer to handle.</typeparam>
    /// <param name="context">Snapshot context to get serializer from.</param>
    /// <returns>Serializer for the specified type.</returns>
    /// <exception cref="Exception">
    /// Throw if the serializer for the specified type is not found.
    /// </exception>
    public static SnapshotSerializer<TTarget> RequireSerializer<TTarget>(this SnapshotContext context)
        => context.GetSerializer(typeof(TTarget)) as SnapshotSerializer<TTarget>
           ?? throw new Exception($"Failed to find the required serializer for '{typeof(TTarget)}'.");

    /// <summary>
    /// Register a serializer type for the specified target type.
    /// </summary>
    /// <param name="container">Injection container to register the serializer into.</param>
    /// <typeparam name="TTarget">Type of target instances for the serializer to handle.</typeparam>
    /// <typeparam name="TSerializer">Type of the serializer to register.</typeparam>
    /// <returns>Specified injection container.</returns>
    internal static IInjectionContainer WithSerializer<TTarget, TSerializer>(this IInjectionContainer container)
        where TSerializer : SnapshotSerializer<TTarget>
    {
        container.AddInjection(typeof(SnapshotSerializer<TTarget>), typeof(TSerializer),
            InjectionLifespan.Singleton);
        return container;
    }
}