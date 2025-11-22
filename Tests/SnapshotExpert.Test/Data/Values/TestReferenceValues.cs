using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Test.Data.Values;

[TestFixture]
public class TestReferenceValues
{
    [Test]
    public void InternalReferenceValue_ContentEquals_And_Hash_And_Debugger()
    {
        var root = new SnapshotNode();
        var obj = root.AssignValue(new ObjectValue());
        var a = obj.CreateNode("a");

        var r1 = new InternalReferenceValue(a);
        var r2 = new InternalReferenceValue(a);
        var r3 = new InternalReferenceValue(root);
        var rNull = new InternalReferenceValue();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(r1.ContentEquals(r2), Is.True);
            Assert.That(r1.ContentEquals(r3), Is.False);
            Assert.That(r1.ContentEquals(null), Is.False);
        }

        // Hash codes: when reference is set, equals referenced node's hash; otherwise default type hash
        Assert.That(r1.GetContentHashCode(), Is.EqualTo(a.GetHashCode()));
        Assert.That(rNull.GetContentHashCode(), Is.EqualTo(typeof(InternalReferenceValue).GetHashCode()));

        // Debugger string should include the referenced path or null
        Assert.That(r1.DebuggerString, Does.Contain(a.Path));
        Assert.That(rNull.DebuggerString, Does.Contain("null"));
    }

    [Test]
    public void ExternalReferenceValue_ContentEquals_And_Hash_And_Debugger()
    {
        var e1 = new ExternalReferenceValue("id-123");
        var e2 = new ExternalReferenceValue("id-123");
        var e3 = new ExternalReferenceValue("id-456");
        var eNull = new ExternalReferenceValue();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(e1.ContentEquals(e2), Is.True);
            Assert.That(e1.ContentEquals(e3), Is.False);
            Assert.That(e1.ContentEquals(null), Is.False);
        }

        // Hash codes: when identifier is set, equals string's hash; otherwise default type hash
        Assert.That(e1.GetContentHashCode(), Is.EqualTo("id-123".GetHashCode()));
        Assert.That(eNull.GetContentHashCode(), Is.EqualTo(typeof(ExternalReferenceValue).GetHashCode()));

        // Debugger string should include the identifier or null
        Assert.That(e1.DebuggerString, Does.Contain("id-123"));
        Assert.That(eNull.DebuggerString, Does.Contain("null"));
    }
}