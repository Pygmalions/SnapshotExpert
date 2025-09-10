using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Values.Primitives;
using SnapshotExpert.Serializers.Generics;

namespace SnapshotExpert.Test.Serializers.Generics;

[TestFixture, TestOf(typeof(NullableValueSnapshotSerializer<>))]
public class TestNullableSnapshotSerializer
{
    [Test]
    public void SaveSnapshot_NotNull()
    {
        var context = new SnapshotContext();

        var serializer = context.RequireSerializer<int?>();

        int? value = TestContext.CurrentContext.Random.Next();
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        Assert.Multiple(() =>
        {
            Assert.That(node.Value, Is.TypeOf<Integer32Value>());
            Assert.That(((Integer32Value)node.Value!).Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void SaveSnapshot_Null()
    {
        var context = new SnapshotContext();

        var serializer = context.RequireSerializer<int?>();

        int? value = null;
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        Assert.Multiple(() => { Assert.That(node.Value, Is.TypeOf<NullValue>()); });
    }

    [Test]
    public void LoadSnapshot_NotNull()
    {
        var context = new SnapshotContext();

        var serializer = context.RequireSerializer<int?>();

        int? value = TestContext.CurrentContext.Random.Next();
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        Assert.Multiple(() =>
        {
            Assert.That(node.Value, Is.TypeOf<Integer32Value>());
            Assert.That(((Integer32Value)node.Value!).Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void LoadSnapshot_Null()
    {
        var context = new SnapshotContext();

        var serializer = context.RequireSerializer<int?>();

        int? value = null;
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);
        Assert.Multiple(() => { Assert.That(node.Value, Is.TypeOf<NullValue>()); });
    }
}