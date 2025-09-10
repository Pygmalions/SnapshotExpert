using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;
using SnapshotExpert.Serializers.Generics;

namespace SnapshotExpert.Test.Serializers.Generics;

[TestFixture, TestOf(typeof(NullableValueSnapshotSerializer<>))]
public class TestKeyValuePairSnapshotSerializer
{
    [Test]
    public void SaveSnapshot()
    {
        var context = new SnapshotContext();

        var serializer = context.RequireSerializer<KeyValuePair<string, int>>();

        var value = new KeyValuePair<string, int>(
            TestContext.CurrentContext.Random.GetString(
                TestContext.CurrentContext.Random.Next(5, 10)
            ),
            TestContext.CurrentContext.Random.Next()
        );
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        Assert.Multiple(() =>
        {
            Assert.That(node.Value, Is.TypeOf<ObjectValue>());
            var objectValue = (ObjectValue)node.Value!;
            Assert.That((objectValue["Key"]?.Value as StringValue)?.Value, Is.EqualTo(value.Key));
            Assert.That((objectValue["Value"]?.Value as Integer32Value)?.Value, Is.EqualTo(value.Value));
        });
    }

    [Test]
    public void LoadSnapshot()
    {
        var context = new SnapshotContext();

        var serializer = context.RequireSerializer<KeyValuePair<string, int>>();

        var value = new KeyValuePair<string, int>(
            TestContext.CurrentContext.Random.GetString(
                TestContext.CurrentContext.Random.Next(5, 10)
            ),
            TestContext.CurrentContext.Random.Next()
        );

        var node = new SnapshotNode();
        var objectValue = node.AssignObject();
        objectValue.CreateNode("Key").AssignValue(value.Key);
        objectValue.CreateNode("Value").AssignValue(value.Value);

        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);
    }
}