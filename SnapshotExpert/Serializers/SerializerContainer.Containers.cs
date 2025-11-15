using InjectionExpert;
using SnapshotExpert.Serializers.Containers;

namespace SnapshotExpert.Serializers;

internal static class SerializerContainerExtensionsForContainers
{
    private static SnapshotSerializer? CreateSerializerForContainer(
        Type targetType, IInjectionProvider provider)
    {
        Type? serializerType = null;

        if (serializerType is null &&
            !DictionaryByInterfaceSnapshotSerializer.MatchSerializerType(targetType, out  serializerType) &&
            !ListByInterfaceSnapshotSerializer.MatchSerializerType(targetType, out serializerType) &&
            !SetByInterfaceSnapshotSerializer.MatchSerializerType(targetType, out serializerType))
            return null;
        
        return (SnapshotSerializer?)provider.NewObject(serializerType);
    }
    
    /// <summary>
    /// Add snapshot serialization support for well-known containers into this serializer container.
    /// </summary>
    public static TContainer UseContainerSerializers<TContainer>(this TContainer container) 
        where TContainer : ISerializerContainer
    {
        container.Factories.Add(CreateSerializerForContainer);
        return container;
    }
}