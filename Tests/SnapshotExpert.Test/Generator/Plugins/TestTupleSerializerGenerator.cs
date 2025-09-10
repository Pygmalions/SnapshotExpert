using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;
using SnapshotExpert.Generator.Plugins;

namespace SnapshotExpert.Test.Generator.Plugins;

[TestFixture, TestOf(typeof(TupleSerializerGenerator))]
public class TestTupleSerializerGenerator
{
    private SnapshotContext _context;

    [SetUp]
    public void Initialize()
    {
        _context = new SnapshotContext();
    }

    [Test]
    public void GenerateSerializerInstance()
    {
        var serializer = _context.RequireSerializer<Tuple<int, int>>();
        Assert.That(serializer, Is.Not.Null);
    }

    [Test]
    public void SaveSnapshot()
    {
        var serializer = _context.RequireSerializer<Tuple<int, int>>();

        var value = new Tuple<int, int>(TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next());
        
        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node);

        Assert.That(node.Value, Is.InstanceOf<ArrayValue>());
        var values = node.RequireValue<ArrayValue>()
            .Nodes
            .Select(subnode => subnode.Value)
            .OfType<Integer32Value>()
            .Select(subvalue => subvalue.Value);
        Assert.That(values, Is.EquivalentTo(new [] { value.Item1, value.Item2 }));
    }

    [Test]
    public void LoadSnapshot_NotNull()
    {
        var serializer = _context.RequireSerializer<Tuple<int, int>>();

        var value = new Tuple<int, int>(TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next());
        
        var node = new SnapshotNode();
        node.AssignArray([ 
            new Integer32Value(value.Item1),
            new Integer32Value(value.Item2)
        ]);
        
        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);
        Assert.That(restored, Is.EqualTo(value));
    }
    
    [Test]
    public void LoadSnapshot_Null()
    {
        var serializer = _context.RequireSerializer<Tuple<int, int>>();

        var value = new Tuple<int, int>(TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next());
        
        var node = new SnapshotNode();
        node.AssignArray([ 
            new Integer32Value(value.Item1),
            new Integer32Value(value.Item2)
        ]);

        Tuple<int, int> restored = null!;
        serializer.LoadSnapshot(ref restored, node);
        Assert.That(restored, Is.EqualTo(value));
    }
}