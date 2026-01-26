using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestArraySerializer
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();
        Assert.That(container.RequireSerializer<int[]>(),
            Is.TypeOf<ArraySnapshotSerializer<int>>());
    }

    [Test]
    public void SaveSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = new int[]
        {
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next()
        };

        var serializer = container.RequireSerializer<int[]>();
        serializer.SaveSnapshot(value, node);
        Assert.That(node.Value, Is.TypeOf<ArrayValue>());

        using (Assert.EnterMultipleScope())
        {
            var array = node.AsArray;
            Assert.That(array.Count == 3);
            Assert.That(array[0].Value.AsInt32, Is.EqualTo(value[0]));
            Assert.That(array[1].Value.AsInt32, Is.EqualTo(value[1]));
            Assert.That(array[2].Value.AsInt32, Is.EqualTo(value[2]));
        }
    }

    [Test]
    public void LoadSnapshot()
    {
        var container = new SerializerContainer();

        var value = new[]
        {
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next()
        };
        var node = new SnapshotNode
        {
            Value = new ArrayValue(value.Select(number => new Integer32Value(number)))
        };

        var serializer = container.RequireSerializer<int[]>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<ArrayValue>());
            Assert.That(deserialized, Is.EqualTo(value));
        }
    }
}