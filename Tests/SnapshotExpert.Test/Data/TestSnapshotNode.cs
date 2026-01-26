using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Test.Data;

[TestFixture]
public class TestSnapshotNode
{
    [Test]
    public void Path_Constructs_From_Root_To_Node()
    {
        var root = new SnapshotNode(); // name defaults to "#"
        var obj = root.AssignValue(new ObjectValue());
        var a = obj.CreateNode("a");
        var b = obj.CreateNode("b");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.Path, Is.EqualTo("#"));
            Assert.That(a.Path, Is.EqualTo("#/a"));
            Assert.That(b.Path, Is.EqualTo("#/b"));
        }

        // nested level
        var aObj = a.AssignValue(new ObjectValue());
        var c = aObj.CreateNode("c");
        Assert.That(c.Path, Is.EqualTo("#/a/c"));
    }

    [Test]
    public void Path_Cache_And_Invalidation_On_Rename()
    {
        var root = new SnapshotNode();
        var obj = root.AssignValue(new ObjectValue());
        var a = obj.CreateNode("a");
        var aObj = a.AssignValue(new ObjectValue());
        var b = aObj.CreateNode("b");

        // Touch cache
        _ = b.Path;

        // Rename root and ensure child paths are invalidated and recomputed
        root.Name = "root"; // internal setter is visible to tests
        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.Path, Is.EqualTo("root"));
            Assert.That(a.Path, Is.EqualTo("root/a"));
            Assert.That(b.Path, Is.EqualTo("root/a/b"));
        }

        // Rename middle node
        a.Name = "x";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(a.Path, Is.EqualTo("root/x"));
            Assert.That(b.Path, Is.EqualTo("root/x/b"));
        }
    }

    [Test]
    public void Locate_String_Absolute_And_Special_Tokens()
    {
        var root = new SnapshotNode();
        var obj = root.AssignValue(new ObjectValue());
        var a = obj.CreateNode("a");
        var aObj = a.AssignValue(new ObjectValue());
        var b = aObj.CreateNode("b");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.Locate("#/a/b"), Is.SameAs(b));

            // Special tokens
            Assert.That(root.Locate(""), Is.Null);
            Assert.That(root.Locate("."), Is.SameAs(root));
            Assert.That(root.Locate(".."), Is.Null); // root has no parent

            // Unknown child
            Assert.That(root.Locate("#/nope"), Is.Null);

            // Empty segment becomes null current
            Assert.That(root.Locate("#//a"), Is.Null);
        }
    }

    [Test]
    public void Locate_Enumerable_Requires_Matching_Root_Then_Traverse()
    {
        var root = new SnapshotNode();
        var obj = root.AssignValue(new ObjectValue());
        var a = obj.CreateNode("a");
        var aObj = a.AssignValue(new ObjectValue());
        var b = aObj.CreateNode("b");

        using (Assert.EnterMultipleScope())
        {
            // First segment must equal current node name
            Assert.That(root.Locate(new[] { "wrong", "a", "b" }), Is.Null);

            // Relative tokens
            Assert.That(root.Locate(new[] { "#", "a", ".", "..", "a", "b" }), Is.SameAs(b));

            // Going above root yields null in the middle, and finally null
            Assert.That(root.Locate(new[] { "#", "..", "a" }), Is.Null);
        }
    }
}