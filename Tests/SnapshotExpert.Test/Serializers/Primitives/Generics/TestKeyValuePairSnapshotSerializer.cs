using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives.Generics;

[TestFixture, TestOf(typeof(NullableValueSnapshotSerializer<>))]
public class TestKeyValuePairSnapshotSerializer
{
    [Test]
    public void SaveSnapshot()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<KeyValuePair<string, int>>();

        var value = new KeyValuePair<string, int>(
            TestContext.CurrentContext.Random.GetString(
                TestContext.CurrentContext.Random.Next(5, 10)
            ),
            TestContext.CurrentContext.Random.Next()
        );
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<ObjectValue>());
            var objectValue = (ObjectValue)node.Value!;
            Assert.That((objectValue["Key"] as StringValue)?.Value, Is.EqualTo(value.Key));
            Assert.That((objectValue["Value"] as Integer32Value)?.Value, Is.EqualTo(value.Value));
        }
    }

    [Test]
    public void LoadSnapshot()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<KeyValuePair<string, int>>();

        var value = new KeyValuePair<string, int>(
            TestContext.CurrentContext.Random.GetString(
                TestContext.CurrentContext.Random.Next(5, 10)
            ),
            TestContext.CurrentContext.Random.Next()
        );

        var node = new SnapshotNode();
        var objectValue = node.AssignValue(new ObjectValue());
        objectValue.CreateNode("Key").BindValue(value.Key);
        objectValue.CreateNode("Value").BindValue(value.Value);

        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);
    }
}