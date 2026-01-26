using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives;

namespace SnapshotExpert.Test.Serializers.Primitives;

[TestFixture]
public class TestFloatSerializers
{
    [Test]
    public void Resolve()
    {
        var container = new SerializerContainer();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(container.RequireSerializer<float>(), Is.TypeOf<Float32SnapshotSerializer>());
            Assert.That(container.RequireSerializer<double>(), Is.TypeOf<Float64SnapshotSerializer>());
        }
    }

    [Test]
    public void SaveSnapshot_Float()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = 3.14f;
        var serializer = container.RequireSerializer<float>();

        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsFloat, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot_Float()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = 3.14f;
        node.Value = new Float64Value(value);

        var serializer = container.RequireSerializer<float>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }

    [Test]
    public void SaveSnapshot_Double()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = 3.1415926535;
        var serializer = container.RequireSerializer<double>();

        serializer.SaveSnapshot(value, node);
        Assert.That(node.AsDouble, Is.EqualTo(value));
    }

    [Test]
    public void LoadSnapshot_Double()
    {
        var container = new SerializerContainer();
        var node = new SnapshotNode();
        var value = 3.1415926535;
        node.Value = new Float64Value(value);

        var serializer = container.RequireSerializer<double>();
        serializer.NewInstance(out var deserialized);
        serializer.LoadSnapshot(ref deserialized, node);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}