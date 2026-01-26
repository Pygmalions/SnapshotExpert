using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestTimeSpanSerializer
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();
        Assert.That(container.RequireSerializer<TimeSpan>(), Is.TypeOf<TimeSpanSnapshotSerializer>());
    }

    [Test]
    public void SaveSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = TimeSpan.FromMinutes(42);
        var serializer = container.RequireSerializer<TimeSpan>();

        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsString, Is.EqualTo(value.ToString()));
    }

    [Test]
    public void LoadSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = TimeSpan.FromMinutes(42);
        node.Value = new StringValue(value.ToString());

        var serializer = container.RequireSerializer<TimeSpan>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}