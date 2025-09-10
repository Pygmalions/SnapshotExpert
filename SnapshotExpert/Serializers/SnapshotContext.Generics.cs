using SnapshotExpert.Serializers.Generics;

namespace SnapshotExpert.Serializers;

internal static class SnapshotContextExtensionForGenerics
{
    public static Type? GetSerializerType(Type targetType)
    {
        if (!targetType.IsGenericType)
            return null;

        var targetDefinition = targetType.GetGenericTypeDefinition();

        if (targetDefinition == typeof(Nullable<>))
            return typeof(NullableValueSnapshotSerializer<>)
                .MakeGenericType(targetType.GetGenericArguments());

        if (targetDefinition == typeof(KeyValuePair<,>))
            return typeof(KeyValuePairSnapshotSerializer<,>)
                .MakeGenericType(targetType.GetGenericArguments());

        return null;
    }
}