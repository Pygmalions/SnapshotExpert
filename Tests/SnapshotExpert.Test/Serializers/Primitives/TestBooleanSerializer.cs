using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestBooleanSerializer
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();

        Assert.That(container.RequireSerializer<bool>(), Is.TypeOf<BooleanSnapshotSerializer>());
    }

    [Test]
    public void SaveSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = TestContext.CurrentContext.Random.NextBool();

        var serializer = container.RequireSerializer<bool>();
        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsBoolean, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = TestContext.CurrentContext.Random.NextBool();
        node.Value = new BooleanValue(value);

        var serializer = container.RequireSerializer<bool>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}