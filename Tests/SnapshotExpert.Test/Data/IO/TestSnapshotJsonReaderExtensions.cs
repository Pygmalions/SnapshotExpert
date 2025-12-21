using SnapshotExpert.Data;
using SnapshotExpert.Data.IO;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Test.Data.IO;

[TestFixture, TestOf(typeof(SnapshotJsonReaderExtensions))]
public class TestSnapshotJsonReaderExtensions
{
    [Test]
    public void Parse_JsonObject()
    {
        const string text = "{\"Value1\": 1, \"Value2\": 2}";

        var node = SnapshotNode.ParseFromJsonText(text);
        var value = node.Value as ObjectValue;
        var value1 = value?.GetNode("Value1")?.Value;
        var value2 = value?.GetNode("Value2")?.Value;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value, Is.TypeOf<ObjectValue>());
            Assert.That(value1, Is.TypeOf<Integer32Value>());
            Assert.That((value1 as Integer32Value)?.Value, Is.EqualTo(1));
            Assert.That(value2, Is.TypeOf<Integer32Value>());
            Assert.That((value2 as Integer32Value)?.Value, Is.EqualTo(2));
        }
    }

    [Test]
    public void Parse_JsonArray()
    {
        const string text = "[1, 2]";

        var node = SnapshotNode.ParseFromJsonText(text);
        var value = node.Value as ArrayValue;
        var value1 = value?.GetNode(0)?.Value;
        var value2 = value?.GetNode(1)?.Value;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value, Is.TypeOf<ArrayValue>());
            Assert.That(value1, Is.TypeOf<Integer32Value>());
            Assert.That((value1 as Integer32Value)?.Value, Is.EqualTo(1));
            Assert.That(value2, Is.TypeOf<Integer32Value>());
            Assert.That((value2 as Integer32Value)?.Value, Is.EqualTo(2));
        }
    }

    [Test]
    public void Parse_JsonFloat()
    {
        const string text = "0.3";

        var node = SnapshotNode.ParseFromJsonText(text);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Value, Is.TypeOf<Float64Value>());
            Assert.That((node.Value as Float64Value)?.Value, Is.EqualTo(0.3));
        }
    }
}