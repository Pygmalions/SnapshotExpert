using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestIntegerSerializers
{
    private static readonly object[] IntegerTestData =
    [
        new object[]
        {
            typeof(sbyte), (sbyte)42, typeof(Integer8SnapshotSerializer), (Func<SnapshotNode, object>)(n => n.AsSByte)
        },
        new object[]
        {
            typeof(byte), (byte)42, typeof(UnsignedInteger8SnapshotSerializer),
            (Func<SnapshotNode, object>)(n => n.AsByte)
        },
        new object[]
        {
            typeof(short), (short)42, typeof(Integer16SnapshotSerializer), (Func<SnapshotNode, object>)(n => n.AsInt16)
        },
        new object[]
        {
            typeof(ushort), (ushort)42, typeof(UnsignedInteger16SnapshotSerializer),
            (Func<SnapshotNode, object>)(n => n.AsUInt16)
        },
        new object[]
            { typeof(int), 42, typeof(Integer32SnapshotSerializer), (Func<SnapshotNode, object>)(n => n.AsInt32) },
        new object[]
        {
            typeof(uint), 42u, typeof(UnsignedInteger32SnapshotSerializer),
            (Func<SnapshotNode, object>)(n => n.AsUInt32)
        },
        new object[]
            { typeof(long), 42L, typeof(Integer64SnapshotSerializer), (Func<SnapshotNode, object>)(n => n.AsInt64) },
        new object[]
        {
            typeof(ulong), 42UL, typeof(UnsignedInteger64SnapshotSerializer),
            (Func<SnapshotNode, object>)(n => n.AsUInt64)
        },
    ];

    [Test]
    [TestCaseSource(nameof(IntegerTestData))]
    public void Resolve(Type type, object value, Type serializerType, Func<SnapshotNode, object> accessor)
    {
        var container = new SerializerContainer();
        Assert.That(container.RequireSerializer(type), Is.TypeOf(serializerType));
    }

    [Test]
    [TestCaseSource(nameof(IntegerTestData))]
    public void SaveSnapshot(Type type, object value, Type serializerType, Func<SnapshotNode, object> accessor)
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var serializer = container.RequireSerializer(type);

        serializer.SaveSnapshotAsObject(value, node, new SnapshotWritingScope());
        Assert.That(accessor(node), Is.EqualTo(value));
    }

    [Test]
    [TestCaseSource(nameof(IntegerTestData))]
    public void LoadSnapshot(Type type, object value, Type serializerType, Func<SnapshotNode, object> accessor)
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();

        if (value is long or ulong)
            node.Value = new Integer64Value(Convert.ToInt64(value));
        else
            node.Value = new Integer32Value(Convert.ToInt32(value));

        var serializer = container.RequireSerializer(type);
        serializer.NewInstanceAsObject(out var deserialized);
        serializer.LoadSnapshotAsObject(ref deserialized, node, new SnapshotReadingScope(node));
        Assert.That(deserialized, Is.EqualTo(value));
    }
}