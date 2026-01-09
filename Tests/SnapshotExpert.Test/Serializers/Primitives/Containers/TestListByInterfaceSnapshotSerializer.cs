using System.Collections;
using SnapshotExpert.Data;
using SnapshotExpert.Serializers;
using SnapshotExpert.Serializers.Containers;

namespace SnapshotExpert.Test.Serializers.Primitives.Containers;

[TestFixture, TestOf(typeof(ListByInterfaceSnapshotSerializer<,,>))]
public class TestListByInterfaceSnapshotSerializer
{
    private class StubList<TElement> : IList<TElement>
    {
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            throw new NotSupportedException();
        }

        public void Add(TElement item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TElement item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(TElement[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public bool Remove(TElement item)
        {
            throw new NotSupportedException();
        }

        public int Count => throw new NotSupportedException();

        public bool IsReadOnly => throw new NotSupportedException();

        public int IndexOf(TElement item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, TElement item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public TElement this[int index]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }

    [Test,
     TestCase(typeof(List<int>),
         ExpectedResult = typeof(ListByInterfaceSnapshotSerializer<int, List<int>, List<int>>)),
     TestCase(typeof(StubList<int>),
         ExpectedResult = typeof(ListByInterfaceSnapshotSerializer<int, StubList<int>, StubList<int>>)),
     TestCase(typeof(IList<int>),
         ExpectedResult = typeof(ListByInterfaceSnapshotSerializer<int, IList<int>, List<int>>)),
     TestCase(typeof(IReadOnlyList<int>),
         ExpectedResult = typeof(SerializerRedirector<IReadOnlyList<int>>))]
    public Type GenerateSerializerInstance(Type type)
    {
        var context = new SerializerContainer();
        return context.GetSerializer(type)?.GetType() ?? typeof(void);
    }

    public class NumberClass(int number)
    {
        public int Number => number;
    }

    [Test]
    public void SaveSnapshotAndLoadSnapshot_Class()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<List<NumberClass>>();

        var target = new List<NumberClass>()
        {
            new(TestContext.CurrentContext.Random.Next()),
            new(TestContext.CurrentContext.Random.Next()),
            new(TestContext.CurrentContext.Random.Next()),
            new(TestContext.CurrentContext.Random.Next()),
        };

        var node = new SnapshotNode();

        serializer.SaveSnapshot(target, node);

        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored.Select(element => element.Number),
            Is.EquivalentTo(target.Select(element => element.Number)));
    }

    [Test]
    public void SaveSnapshotAndLoadSnapshot_IntoEmpty()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<List<int>>();

        var target = new List<int>()
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

    [Test]
    public void SaveSnapshotAndLoadSnapshot_IntoNotEmpty_LessThanSnapshot_Add()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<List<int>>();

        var target = new List<int>
        {
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
        };

        var node = new SnapshotNode();

        serializer.SaveSnapshot(target, node);

        var restored = new List<int>
        {
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
        };

        serializer.LoadSnapshot(ref restored, node);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored, Is.EquivalentTo(target));
    }

    [Test]
    public void SaveSnapshotAndLoadSnapshot_IntoNotEmpty_MoreThanSnapshot_Remove()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<List<int>>();

        var target = new List<int>
        {
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
        };

        var node = new SnapshotNode();

        serializer.SaveSnapshot(target, node);

        var restored = new List<int>
        {
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.Next(),
        };

        serializer.LoadSnapshot(ref restored, node);

        Assert.That(restored, Is.Not.Null);
        Assert.That(restored, Is.EquivalentTo(target));
    }
}