using InjectionExpert;

namespace SnapshotExpert;

public interface ISerializerContainer : ISerializerProvider
{
    /// <summary>
    /// Factory delegate to create serializers.
    /// </summary>
    public delegate SnapshotSerializer? FactoryDelegate(Type targetType, IInjectionProvider provider);
    
    /// <summary>
    /// List of factories registered in this container.
    /// These factories will be queried if no serializer is registered for the target type through type matching.
    /// Serializers created by factories will be cached.
    /// </summary>
    IList<FactoryDelegate> Factories { get; }
    
    /// <summary>
    /// Register a serializer for the specified target type.
    /// </summary>
    /// <param name="targetType">Target type for the serializer to handle.</param>
    /// <param name="serializer">Serializer to register.</param>
    void AddSerializer(Type targetType, SnapshotSerializer serializer);
    
    /// <summary>
    /// Register a serializer for the specified target type.
    /// </summary>
    /// <param name="targetType">Target type for the serializer to handle.</param>
    /// <param name="serializerType">Type of the serializer to register.</param>
    void AddSerializer(Type targetType, Type serializerType);
    
    /// <summary>
    /// Remove the serializer for the specified type.
    /// </summary>
    /// <param name="targetType">Target type of the serializer to remove.</param>
    void RemoveSerializer(Type targetType);
    
    /// <summary>
    /// Invalidate all cached serializers created from types or factories.
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Clear all registered serializers and factories.
    /// </summary>
    void Clear();
}