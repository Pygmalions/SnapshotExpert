using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Test.Data;

[TestFixture]
public class TestNumberInterface
{
    [Test]
    public void Integer32Serializer_AcceptFloat64()
    {
        var context = new SerializerContainer();
        var serializer = context.RequireSerializer<int>();

        var node = new SnapshotNode
        {
            Value = new Float64Value(1.0)
        };

        var deserialized = 0;
        serializer.LoadSnapshot(ref deserialized, node);
        
        Assert.That(deserialized, Is.EqualTo(1));
    }
    
    [Test]
    public void Float64Serializer_AcceptInteger32()
    {
        var context = new SerializerContainer();
        var serializer = context.RequireSerializer<double>();

        var node = new SnapshotNode
        {
            Value = new Integer32Value(1)
        };

        double deserialized = 0;
        serializer.LoadSnapshot(ref deserialized, node);
        
        Assert.That(deserialized, Is.EqualTo(1.0));
    }
    
    [Test]
    public void Float64Serializer_AcceptInteger64()
    {
        var context = new SerializerContainer();
        var serializer = context.RequireSerializer<double>();

        var node = new SnapshotNode
        {
            Value = new Integer64Value(1)
        };

        double deserialized = 0;
        serializer.LoadSnapshot(ref deserialized, node);
        
        Assert.That(deserialized, Is.EqualTo(1.0));
    }
}