using System.Collections.Concurrent;
using SnapshotExpert.Data;
using SnapshotExpert.Serializers;
using SnapshotExpert.Serializers.Containers;

namespace SnapshotExpert.Test.Serializers.Primitives.Containers;

[TestFixture, TestOf(typeof(DictionaryByInterfaceSnapshotSerializer<,,,>))]
public class TestDictionaryByInterfaceSnapshotSerializer
{
    [Test,
     TestCase(typeof(Dictionary<int, string>),
         ExpectedResult = typeof(DictionaryByInterfaceSnapshotSerializer<
             int, string, Dictionary<int, string>, Dictionary<int, string>>)),
     TestCase(typeof(ConcurrentDictionary<int, string>),
         ExpectedResult = typeof(DictionaryByInterfaceSnapshotSerializer<
             int, string, ConcurrentDictionary<int, string>, ConcurrentDictionary<int, string>>)),
     TestCase(typeof(IDictionary<int, string>),
         ExpectedResult = typeof(DictionaryByInterfaceSnapshotSerializer<
             int, string, IDictionary<int, string>, Dictionary<int, string>>)),
     TestCase(typeof(IReadOnlyDictionary<int, string>),
         ExpectedResult = typeof(SerializerRedirector<IReadOnlyDictionary<int, string>>))]
    public Type GenerateSerializerInstance(Type type)
    {
        var context = new SerializerContainer();
        return context.GetSerializer(type)?.GetType() ?? typeof(void);
    }

    [Test]
    public void SaveSnapshotAndLoadSnapshot_IntoEmpty()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<Dictionary<int, string>>();

        var target = new Dictionary<int, string>()
        {
            { 0, TestContext.CurrentContext.Random.GetString() },
            { 1, TestContext.CurrentContext.Random.GetString() },
            { 2, TestContext.CurrentContext.Random.GetString() },
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
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<Dictionary<int, string>>();

        var target = new Dictionary<int, string>
        {
            { 0, TestContext.CurrentContext.Random.GetString() },
            { 1, TestContext.CurrentContext.Random.GetString() },
            { 2, TestContext.CurrentContext.Random.GetString() },
        };

        var node = new SnapshotNode();

        serializer.SaveSnapshot(target, node);


        var restored = new Dictionary<int, string>
        {
            { 0, TestContext.CurrentContext.Random.GetString() },
            { 2, TestContext.CurrentContext.Random.GetString() },
            { 4, TestContext.CurrentContext.Random.GetString() },
            { 5, TestContext.CurrentContext.Random.GetString() },
        };

        serializer.LoadSnapshot(ref restored, node);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored, Is.EquivalentTo(target));
    }
}