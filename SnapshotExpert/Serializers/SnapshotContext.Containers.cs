using SnapshotExpert.Serializers.Containers;

namespace SnapshotExpert.Serializers;

internal static class SnapshotContextExtensionForContainers
{
    public static Type? GetSerializerType(Type targetType)
    {
        if (targetType.IsArray && targetType.GetArrayRank() == 1)
            return typeof(ArraySnapshotSerializer<>)
                .MakeGenericType(targetType.GetElementType()!);

        if (DictionaryByInterfaceSnapshotSerializer.MatchSerializerType(targetType, out var serializerType) ||
            ListByInterfaceSnapshotSerializer.MatchSerializerType(targetType, out serializerType) ||
            SetByInterfaceSnapshotSerializer.MatchSerializerType(targetType, out serializerType))
            return serializerType;
        
        return null;
    }
}