using SnapshotExpert.Data;
using SnapshotExpert.Serializers;
using SnapshotExpert.Serializers.Containers;

namespace SnapshotExpert.Test.Serializers.Primitives.Containers;

[TestFixture, TestOf(typeof(DictionaryByInterfaceSnapshotSerializer<,,,>))]
public class TestSetByInterfaceSnapshotSerializer
{
    [Test,
     TestCase(typeof(HashSet<int>),
         ExpectedResult = typeof(SetByInterfaceSnapshotSerializer<int, HashSet<int>, HashSet<int>>)),
     TestCase(typeof(ISet<int>),
         ExpectedResult = typeof(SetByInterfaceSnapshotSerializer<int, ISet<int>, HashSet<int>>)),
     TestCase(typeof(IReadOnlySet<int>),
         ExpectedResult = typeof(SerializerRedirector<IReadOnlySet<int>>))]
    public Type GenerateSerializerInstance(Type type)
    {
        var context = new SerializerContainer();
        return context.GetSerializer(type)?.GetType() ?? typeof(void);
    }

    [Test]
    public void SaveSnapshotAndLoadSnapshot()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<HashSet<int>>();

        var target = new HashSet<int>()
        {
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
        };

        var node = new SnapshotNode();

        serializer.SaveSnapshot(target, node);

        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored, Is.EquivalentTo(target));
    }
}