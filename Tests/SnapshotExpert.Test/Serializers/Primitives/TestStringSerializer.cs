using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestStringSerializer
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();

        Assert.That(container.RequireSerializer<string>(), Is.TypeOf<StringSnapshotSerializer>());
    }

    [Test]
    public void SaveSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = TestContext.CurrentContext.Random.GetString(10);

        var serializer = container.RequireSerializer<string>();
        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsString, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = TestContext.CurrentContext.Random.GetString(10);
        node.Value = new StringValue(value);

        var serializer = container.RequireSerializer<string>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}