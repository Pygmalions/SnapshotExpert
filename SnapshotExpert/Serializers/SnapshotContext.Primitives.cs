using InjectionExpert;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Serializers;

internal static class SnapshotContextExtensionForPrimitives
{
    public static void WithPrimitiveSerializers(this IInjectionContainer container)
    {
        container.WithSerializer<bool, BooleanSnapshotSerializer>();
        
        container.WithSerializer<string, StringSnapshotSerializer>();
        container.WithSerializer<char, CharacterSnapshotSerializer>();
        container.WithSerializer<sbyte, Integer8SnapshotSerializer>();
        container.WithSerializer<byte, UnsignedInteger8SnapshotSerializer>();
        container.WithSerializer<short, Integer16SnapshotSerializer>();
        container.WithSerializer<ushort, UnsignedInteger16SnapshotSerializer>();
        container.WithSerializer<int, Integer32SnapshotSerializer>();
        container.WithSerializer<uint, UnsignedInteger32SnapshotSerializer>();
        container.WithSerializer<long, Integer64SnapshotSerializer>();
        container.WithSerializer<ulong, UnsignedInteger64SnapshotSerializer>();
        container.WithSerializer<float, Float32SnapshotSerializer>();
        container.WithSerializer<double, Float64SnapshotSerializer>();
        container.WithSerializer<decimal, DecimalSnapshotSerializer>();
        
        container.WithSerializer<Type, TypeSnapshotSerializer>();
        container.WithSerializer<byte[], ByteArraySnapshotSerializer>();
        
        container.WithSerializer<Guid, GuidSnapshotSerializer>();
        container.WithSerializer<DateTime, DateTimeSnapshotSerializer>();
        container.WithSerializer<DateTimeOffset, DateTimeOffsetSnapshotSerializer>();
        container.WithSerializer<TimeSpan, TimeSpanSnapshotSerializer>();
    }
}