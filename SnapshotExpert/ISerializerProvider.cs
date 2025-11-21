using InjectionExpert;
using SnapshotExpert.Adapters;

namespace SnapshotExpert;

public interface ISerializerProvider
{
    /// <summary>
    /// Get a snapshot serializer for the specified target type.
    /// </summary>
    /// <param name="targetType">Type of instances for the serializer to handle.</param>
    /// <returns>Serializer for the specified type, or null if not found.</returns>
    SnapshotSerializer? GetSerializer(Type targetType);

    public IInjectionProvider AsInjections()
        => new SerializerProviderToInjectionProviderAdapter(this);
}