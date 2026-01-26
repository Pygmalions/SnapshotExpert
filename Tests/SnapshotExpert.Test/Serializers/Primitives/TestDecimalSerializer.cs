using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestDecimalSerializer
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();
        Assert.That(container.RequireSerializer<decimal>(), Is.TypeOf<DecimalSnapshotSerializer>());
    }

    [Test]
    public void SaveSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = 123.456m;
        var serializer = container.RequireSerializer<decimal>();

        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsDecimal, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = 123.456m;
        node.Value = new DecimalValue(value);

        var serializer = container.RequireSerializer<decimal>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}