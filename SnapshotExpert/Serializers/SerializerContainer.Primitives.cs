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
        else if (targetType.IsArray)
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
            .UseSerializer<bool, BooleanSnapshotSerializer>()
            .UseSerializer<string, StringSnapshotSerializer>()
            .UseSerializer<char, CharacterSnapshotSerializer>()
            .UseSerializer<sbyte, Integer8SnapshotSerializer>()
            .UseSerializer<byte, UnsignedInteger8SnapshotSerializer>()
            .UseSerializer<short, Integer16SnapshotSerializer>()
            .UseSerializer<ushort, UnsignedInteger16SnapshotSerializer>()
            .UseSerializer<int, Integer32SnapshotSerializer>()
            .UseSerializer<uint, UnsignedInteger32SnapshotSerializer>()
            .UseSerializer<long, Integer64SnapshotSerializer>()
            .UseSerializer<ulong, UnsignedInteger64SnapshotSerializer>()
            .UseSerializer<float, Float32SnapshotSerializer>()
            .UseSerializer<double, Float64SnapshotSerializer>()
            .UseSerializer<decimal, DecimalSnapshotSerializer>();

        container.UseSerializer<Type, TypeSnapshotSerializer>();
        container.UseSerializer<byte[], ByteArraySnapshotSerializer>();

        container.UseSerializer<Guid, GuidSnapshotSerializer>();
        container.UseSerializer<DateTime, DateTimeSnapshotSerializer>();
        container.UseSerializer<DateTimeOffset, DateTimeOffsetSnapshotSerializer>();
        container.UseSerializer<TimeSpan, TimeSpanSnapshotSerializer>();

        container.UseSerializer<object, SerializerRedirector<object>>();

        container.Factories.Add(CreateSerializerForPrimitive);

        return container;
    }
}