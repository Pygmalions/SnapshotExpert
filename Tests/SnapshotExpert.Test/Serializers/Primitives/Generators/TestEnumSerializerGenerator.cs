using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives.Generators;

namespace SnapshotExpert.Test.Serializers.Primitives.Generators;

[TestFixture, TestOf(typeof(EnumSerializerGenerator))]
public class TestEnumSerializerGenerator
{
    [SetUp]
    public void Initialize()
    {
        _context = new SerializerContainer();
    }

    public enum SampleEnum
    {
        EntryA = 1,
        EntryB = 2,
        EntryC = 3,
        EntryD = 5,
    }

    private SerializerContainer _context;

    [Test]
    public void GenerateSerializerInstance()
    {
        var serializer = _context.RequireSerializer<SampleEnum>();
        Assert.That(serializer, Is.Not.Null);
    }

    [Test,
     TestCase(SampleEnum.EntryA),
     TestCase(SampleEnum.EntryB),
     TestCase(SampleEnum.EntryC),
     TestCase(SampleEnum.EntryD)
    ]
    public void SaveSnapshot_AsInteger(SampleEnum value)
    {
        var serializer = _context.RequireSerializer<SampleEnum>();

        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node, new SnapshotWritingScope()
        {
            Format = SnapshotDataFormat.Binary
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<Integer32Value>());
            Assert.That((node.Value as Integer32Value)?.Value, Is.EqualTo((int)value));
        }
    }

    [Test,
     TestCase(SampleEnum.EntryA),
     TestCase(SampleEnum.EntryB),
     TestCase(SampleEnum.EntryC),
     TestCase(SampleEnum.EntryD)
    ]
    public void SaveSnapshot_AsString(SampleEnum value)
    {
        var serializer = _context.RequireSerializer<SampleEnum>();

        var node = new SnapshotNode();
        serializer.SaveSnapshot(value, node, new SnapshotWritingScope()
        {
            Format = SnapshotDataFormat.Textual
        });
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<StringValue>());
            Assert.That((node.Value as StringValue)?.Value, Is.EqualTo(value.ToString()));
        }
    }

    [Test,
     TestCase(SampleEnum.EntryA),
     TestCase(SampleEnum.EntryB),
     TestCase(SampleEnum.EntryC),
     TestCase(SampleEnum.EntryD)]
    public void LoadSnapshot_FromInteger(SampleEnum value)
    {
        var serializer = _context.RequireSerializer<SampleEnum>();

        var node = new SnapshotNode();
        node.BindValue((int)value);
        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);
        Assert.That(restored, Is.EqualTo(value));
    }

    [Test,
     TestCase(SampleEnum.EntryA),
     TestCase(SampleEnum.EntryB),
     TestCase(SampleEnum.EntryC),
     TestCase(SampleEnum.EntryD)]
    public void LoadSnapshot_FromString(SampleEnum value)
    {
        var serializer = _context.RequireSerializer<SampleEnum>();

        var node = new SnapshotNode();
        node.BindValue(value.ToString());
        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);
        Assert.That(restored, Is.EqualTo(value));
    }
}