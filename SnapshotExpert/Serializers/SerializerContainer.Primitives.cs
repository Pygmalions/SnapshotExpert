using System.Runtime.CompilerServices;
using InjectionExpert;
using SnapshotExpert.Serializers.Primitives;
using SnapshotExpert.Serializers.Primitives.Generators;

namespace SnapshotExpert.Serializers;

internal static class SerializerContainerExtensionsForPrimitives
{
    private static SnapshotSerializer? CreateSerializerForPrimitive(
        Type targetType, IInjectionProvider provider)
    {
        Type? serializerType = null;

        // Check for non-generic types.

        if (targetType.IsEnum)
            serializerType = EnumSerializerGenerator.GetSerializerType(targetType);
        else if (targetType.IsArray && targetType.GetArrayRank() > 1)
        {
            if (targetType.GetArrayRank() == 1)
                serializerType = typeof(ArraySnapshotSerializer<>)
                    .MakeGenericType(targetType.GetElementType()!);
            else
                serializerType = MatrixSerializerGenerator.GetSerializerType(targetType);
        }

        if (serializerType != null)
            return (SnapshotSerializer?)provider.NewObject(serializerType);

        // Check for generic types.

        if (!targetType.IsGenericType)
            return null;
        var targetDefinition = targetType.GetGenericTypeDefinition();

        if (targetDefinition == typeof(Nullable<>))
            serializerType = typeof(NullableValueSnapshotSerializer<>)
                .MakeGenericType(targetType.GetGenericArguments());
        else if (targetDefinition == typeof(KeyValuePair<,>))
            serializerType = typeof(KeyValuePairSnapshotSerializer<,>)
                .MakeGenericType(targetType.GetGenericArguments());
        // Tuples have an unknown number of generic arguments, so they are checked through the interface.
        else if (targetType.IsAssignableTo(typeof(ITuple)))
        {
            serializerType = targetType.IsValueType
                ? ValueTupleSerializerGenerator.GetSerializerType(targetType)
                : TupleSerializerGenerator.GetSerializerType(targetType);
        }

        if (serializerType is null)
            return null;
        return (SnapshotSerializer?)provider.NewObject(serializerType);
    }

    public static TContainer UsePrimitiveSerializers<TContainer>(this TContainer container)
        where TContainer : ISerializerContainer
    {
        container
            .WithSerializer<bool, BooleanSnapshotSerializer>()
            .WithSerializer<string, StringSnapshotSerializer>()
            .WithSerializer<char, CharacterSnapshotSerializer>()
            .WithSerializer<sbyte, Integer8SnapshotSerializer>()
            .WithSerializer<byte, UnsignedInteger8SnapshotSerializer>()
            .WithSerializer<short, Integer16SnapshotSerializer>()
            .WithSerializer<ushort, UnsignedInteger16SnapshotSerializer>()
            .WithSerializer<int, Integer32SnapshotSerializer>()
            .WithSerializer<uint, UnsignedInteger32SnapshotSerializer>()
            .WithSerializer<long, Integer64SnapshotSerializer>()
            .WithSerializer<ulong, UnsignedInteger64SnapshotSerializer>()
            .WithSerializer<float, Float32SnapshotSerializer>()
            .WithSerializer<double, Float64SnapshotSerializer>()
            .WithSerializer<decimal, DecimalSnapshotSerializer>();

        container.WithSerializer<Type, TypeSnapshotSerializer>();
        container.WithSerializer<byte[], ByteArraySnapshotSerializer>();

        container.WithSerializer<Guid, GuidSnapshotSerializer>();
        container.WithSerializer<DateTime, DateTimeSnapshotSerializer>();
        container.WithSerializer<DateTimeOffset, DateTimeOffsetSnapshotSerializer>();
        container.WithSerializer<TimeSpan, TimeSpanSnapshotSerializer>();

        container.Factories.Add(CreateSerializerForPrimitive);

        return container;
    }
}