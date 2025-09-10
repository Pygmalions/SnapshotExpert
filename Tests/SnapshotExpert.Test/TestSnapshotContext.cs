using SnapshotExpert.Serializers.Generics;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test;

[TestFixture, TestOf(typeof(SnapshotContext))]
public class TestSnapshotContext
{
    private SnapshotContext _context;

    [SetUp]
    public void InitializeContext()
    {
        _context = new SnapshotContext();
    }

    [Test,
     TestCase(typeof(bool), ExpectedResult = typeof(BooleanSnapshotSerializer)),
     TestCase(typeof(string), ExpectedResult = typeof(StringSnapshotSerializer)),
     TestCase(typeof(char), ExpectedResult = typeof(CharacterSnapshotSerializer)),
     TestCase(typeof(sbyte), ExpectedResult = typeof(Integer8SnapshotSerializer)),
     TestCase(typeof(byte), ExpectedResult = typeof(UnsignedInteger8SnapshotSerializer)),
     TestCase(typeof(short), ExpectedResult = typeof(Integer16SnapshotSerializer)),
     TestCase(typeof(ushort), ExpectedResult = typeof(UnsignedInteger16SnapshotSerializer)),
     TestCase(typeof(int), ExpectedResult = typeof(Integer32SnapshotSerializer)),
     TestCase(typeof(uint), ExpectedResult = typeof(UnsignedInteger32SnapshotSerializer)),
     TestCase(typeof(long), ExpectedResult = typeof(Integer64SnapshotSerializer)),
     TestCase(typeof(ulong), ExpectedResult = typeof(UnsignedInteger64SnapshotSerializer)),
     TestCase(typeof(float), ExpectedResult = typeof(Float32SnapshotSerializer)),
     TestCase(typeof(double), ExpectedResult = typeof(Float64SnapshotSerializer)),
     TestCase(typeof(decimal), ExpectedResult = typeof(DecimalSnapshotSerializer)),
     TestCase(typeof(byte[]), ExpectedResult = typeof(ByteArraySnapshotSerializer)),
     TestCase(typeof(Type), ExpectedResult = typeof(TypeSnapshotSerializer)),
     TestCase(typeof(Guid), ExpectedResult = typeof(GuidSnapshotSerializer)),
     TestCase(typeof(DateTime), ExpectedResult = typeof(DateTimeSnapshotSerializer)),
     TestCase(typeof(DateTimeOffset), ExpectedResult = typeof(DateTimeOffsetSnapshotSerializer))]
    public Type GetSerializer_Primitive(Type targetType)
    {
        return _context.GetSerializer(targetType)?.GetType() ?? typeof(void);
    }

    [Test, TestCase(typeof(bool?), ExpectedResult = typeof(NullableValueSnapshotSerializer<bool>))]
    public Type GetSerializer_Nullable(Type targetType)
    {
        return _context.GetSerializer(targetType)?.GetType() ?? typeof(void);
    }
}