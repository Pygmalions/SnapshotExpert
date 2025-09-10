using System.Collections.Concurrent;
using SnapshotExpert.Framework;
using SnapshotExpert.Serializers;
using SnapshotExpert.Serializers.Containers;

namespace SnapshotExpert.Test.Serializers.Containers;

[TestFixture, TestOf(typeof(StringDictionaryByInterfaceSnapshotSerializer<,,>))]
public class TestStringDictionaryByInterfaceSnapshotSerializer
{
    [Test,
     TestCase(typeof(Dictionary<string, string>),
         ExpectedResult = typeof(StringDictionaryByInterfaceSnapshotSerializer<
             string, Dictionary<string, string>, Dictionary<string, string>>)),
     TestCase(typeof(ConcurrentDictionary<string, string>),
         ExpectedResult = typeof(StringDictionaryByInterfaceSnapshotSerializer<
             string, ConcurrentDictionary<string, string>, ConcurrentDictionary<string, string>>)),
     TestCase(typeof(IDictionary<string, string>),
         ExpectedResult = typeof(StringDictionaryByInterfaceSnapshotSerializer<
             string, IDictionary<string, string>, Dictionary<string, string>>)),
     TestCase(typeof(IReadOnlyDictionary<string, string>),
         ExpectedResult = typeof(SnapshotSerializerRedirector<IReadOnlyDictionary<string, string>>))]
    public Type GenerateSerializerInstance(Type type)
    {
        var context = new SnapshotContext();
        return context.GetSerializer(type)?.GetType() ?? typeof(void);
    }

    [Test]
    public void SaveSnapshotAndLoadSnapshot_IntoEmpty()
    {
        var context = new SnapshotContext();

        var serializer = context.RequireSerializer<Dictionary<string, string>>();

        var target = new Dictionary<string, string>
        {
            { "0", TestContext.CurrentContext.Random.GetString() },
            { "1", TestContext.CurrentContext.Random.GetString() },
            { "2", TestContext.CurrentContext.Random.GetString() },
        };

        var node = new SnapshotNode();

        serializer.SaveSnapshot(target, node);

        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored, Is.EquivalentTo(target));
    }

    [Test]
    public void SaveSnapshotAndLoadSnapshot_IntoNotEmpty()
    {
        var context = new SnapshotContext();

        var serializer = context.RequireSerializer<Dictionary<string, string>>();

        var target = new Dictionary<string, string>
        {
            { "0", TestContext.CurrentContext.Random.GetString() },
            { "1", TestContext.CurrentContext.Random.GetString() },
            { "2", TestContext.CurrentContext.Random.GetString() },
        };

        var node = new SnapshotNode();

        serializer.SaveSnapshot(target, node);

        var restored = new Dictionary<string, string>
        {
            { "0", TestContext.CurrentContext.Random.GetString() },
            { "2", TestContext.CurrentContext.Random.GetString() },
            { "4", TestContext.CurrentContext.Random.GetString() },
            { "5", TestContext.CurrentContext.Random.GetString() },
        };

        serializer.LoadSnapshot(ref restored, node);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored, Is.EquivalentTo(target));
    }
}