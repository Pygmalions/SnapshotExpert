using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestTypeSerializer
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();
        Assert.That(container.RequireSerializer<Type>(), Is.TypeOf<TypeSnapshotSerializer>());
    }

    [Test]
    public void SaveSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = typeof(string);
        var serializer = container.RequireSerializer<Type>();

        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsString, Is.EqualTo($"{value.FullName}, {value.Assembly.GetName().Name}"));
    }

    [Test]
    public void SaveSnapshot_Null()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        Type value = null!;
        var serializer = container.RequireSerializer<Type>();

        serializer.SaveSnapshot(value, node);
        Assert.That(node.IsNull, Is.True);
    }

    [Test]
    public void LoadSnapshot()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = typeof(int);
        node.Value = new StringValue(value.AssemblyQualifiedName!);

        var serializer = container.RequireSerializer<Type>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot_Null()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        node.Value = new NullValue();

        var serializer = container.RequireSerializer<Type>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.Null);
    }
}