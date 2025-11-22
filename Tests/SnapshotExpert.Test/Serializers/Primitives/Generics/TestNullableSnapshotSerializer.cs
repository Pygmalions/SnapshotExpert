using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives.Generics;

[TestFixture, TestOf(typeof(NullableValueSnapshotSerializer<>))]
public class TestNullableSnapshotSerializer
{
    [Test]
    public void SaveSnapshot_NotNull()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<int?>();

        int? value = TestContext.CurrentContext.Random.Next();
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<Integer32Value>());
            Assert.That(((Integer32Value)node.Value!).Value, Is.EqualTo(value));
        }
    }

    [Test]
    public void SaveSnapshot_Null()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<int?>();

        int? value = null;
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<NullValue>());
        }
    }

    [Test]
    public void LoadSnapshot_NotNull()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<int?>();

        int? value = TestContext.CurrentContext.Random.Next();
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<Integer32Value>());
            Assert.That(((Integer32Value)node.Value!).Value, Is.EqualTo(value));
        }
    }

    [Test]
    public void LoadSnapshot_Null()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<int?>();

        int? value = null;
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<NullValue>());
        }
    }
}