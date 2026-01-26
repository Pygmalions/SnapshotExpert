using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestByteArraySerializer
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();
        Assert.That(container.RequireSerializer<byte[]>(), Is.TypeOf<ByteArraySnapshotSerializer>());
    }

    [Test]
    public void SaveSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = new byte[] { 1, 2, 3, 4, 5 };
        var serializer = container.RequireSerializer<byte[]>();

        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsBytes, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = new byte[] { 5, 4, 3, 2, 1 };
        node.Value = new BinaryValue(value);

        var serializer = container.RequireSerializer<byte[]>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}