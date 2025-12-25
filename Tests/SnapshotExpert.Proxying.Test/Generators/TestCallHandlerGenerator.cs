using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Remoting.Generators;

namespace SnapshotExpert.Remoting.Test.Generators;

[TestFixture, TestOf(typeof(CallHandlerGenerator))]
public class TestCallHandlerGenerator
{
    private class SampleClass
    {
        public int Number { get; set; }

        public int Add(int a, int b) => a + b + Number;

        public async Task<int> AddAsyncTask(int a, int b)
        {
            await Task.Yield();
            return a + b + Number;
        }

        public async ValueTask<int> AddAsyncValueTask(int a, int b)
        {
            await Task.Yield();
            return a + b + Number;
        }

        public void DoNothing(int a, int b)
        {
        }

        public async Task DoNothingAsyncTask(int a, int b)
        {
            await Task.Yield();
        }

        public async ValueTask DoNothingAsyncValueTask(int a, int b)
        {
            await Task.Yield();
        }
    }

    [Test]
    public async Task CreateAndInvoke_Functor()
    {
        var instance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next(0, 100)
        };

        var serializers = new SerializerContainer();

        var handler = CallHandlerGenerator.For(serializers, instance.Add);

        var result = await handler.HandleCall(new ObjectValue(new OrderedDictionary<string, SnapshotValue>()
            {
                { "a", new Integer32Value(1) },
                { "b", new Integer32Value(2) }
            }
        )) as Integer32Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(instance.Add(1, 2)));
    }

    [Test]
    public async Task CreateAndInvoke_AsyncFunctor_Task()
    {
        var instance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next(0, 100)
        };

        var serializers = new SerializerContainer().UseProxyingSerializers();

        var handler = CallHandlerGenerator.For(serializers, instance.AddAsyncTask);

        var result = await handler.HandleCall(new ObjectValue(new OrderedDictionary<string, SnapshotValue>()
            {
                { "a", new Integer32Value(1) },
                { "b", new Integer32Value(2) }
            }
        )) as Integer32Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(instance.Add(1, 2)));
    }

    [Test]
    public async Task CreateAndInvoke_AsyncFunctor_ValueTask()
    {
        var instance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next(0, 100)
        };

        var serializers = new SerializerContainer().UseProxyingSerializers();

        var handler = CallHandlerGenerator.For(serializers, instance.AddAsyncValueTask);

        var result = await handler.HandleCall(new ObjectValue(new OrderedDictionary<string, SnapshotValue>()
            {
                { "a", new Integer32Value(1) },
                { "b", new Integer32Value(2) }
            }
        )) as Integer32Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(instance.Add(1, 2)));
    }

    [Test]
    public async Task CreateAndInvoke_Action()
    {
        var instance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next(0, 100)
        };

        var serializers = new SerializerContainer();

        var handler = CallHandlerGenerator.For(serializers, instance.DoNothing);

        var result = await handler.HandleCall(new ObjectValue(new OrderedDictionary<string, SnapshotValue>()
            {
                { "a", new Integer32Value(1) },
                { "b", new Integer32Value(2) }
            }
        ));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAndInvoke_AsyncAction_Task()
    {
        var instance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next(0, 100)
        };

        var serializers = new SerializerContainer();

        var handler = CallHandlerGenerator.For(serializers, instance.DoNothingAsyncTask);

        var result = await handler.HandleCall(new ObjectValue(new OrderedDictionary<string, SnapshotValue>()
            {
                { "a", new Integer32Value(1) },
                { "b", new Integer32Value(2) }
            }
        ));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task CreateAndInvoke_AsyncAction_ValueTask()
    {
        var instance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next(0, 100)
        };

        var serializers = new SerializerContainer();

        var handler = CallHandlerGenerator.For(serializers, instance.DoNothingAsyncValueTask);

        var result = await handler.HandleCall(new ObjectValue(new OrderedDictionary<string, SnapshotValue>()
            {
                { "a", new Integer32Value(1) },
                { "b", new Integer32Value(2) }
            }
        ));

        Assert.That(result, Is.Null);
    }

    [Test]
    public void CreateAndInvoke_Cancel_AsyncAction_ValueTask()
    {
        var instance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next(0, 100)
        };

        var serializers = new SerializerContainer();

        var handler = CallHandlerGenerator.For(serializers, instance.DoNothingAsyncValueTask);

        var cancellationSource = new CancellationTokenSource();

        cancellationSource.Cancel();

        var result = handler.HandleCall(new ObjectValue(new OrderedDictionary<string, SnapshotValue>()
            {
                { "a", new Integer32Value(1) },
                { "b", new Integer32Value(2) }
            }
        ), cancellationSource.Token);

        Assert.That(result.IsCanceled, Is.True);
    }
}