using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestGuidSerializer
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();
        Assert.That(container.RequireSerializer<Guid>(), Is.TypeOf<GuidSnapshotSerializer>());
    }

    [Test]
    public void SaveSnapshot_Binary()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = Guid.NewGuid();
        var serializer = container.RequireSerializer<Guid>();

        // Default is binary
        serializer.SaveSnapshot(value, node);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.AsGuid, Is.EqualTo(value));
            Assert.That(node.Value, Is.TypeOf<BinaryValue>());
        }
    }

    [Test]
    public void SaveSnapshot_Textual()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = Guid.NewGuid();
        var serializer = container.RequireSerializer<Guid>();

        var scope = new SnapshotWritingScope { Format = SnapshotDataFormat.Textual };
        serializer.SaveSnapshot(value, node, scope);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.AsGuid, Is.EqualTo(value));
            Assert.That(node.Value, Is.TypeOf<StringValue>());
        }
    }

    [Test]
    public void LoadSnapshot_Binary()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = Guid.NewGuid();
        node.Value = new BinaryValue(value);

        var serializer = container.RequireSerializer<Guid>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot_String()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = Guid.NewGuid();
        node.Value = new StringValue(value.ToString());

        var serializer = container.RequireSerializer<Guid>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}