using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Remoting.Generators;

namespace SnapshotExpert.Remoting.Test.Generators;

[TestFixture, TestOf(typeof(CallProxyGenerator))]
public class TestCallProxyGenerator
{
    private class SampleClass
    {
        public int Add(int a, int b) => a + b;

        public async Task<int> AddAsyncTask(int a, int b)
        {
            await Task.Yield();
            return a + b;
        }

        public async ValueTask<int> AddAsyncValueTask(int a, int b)
        {
            await Task.Yield();
            return a + b;
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
    public void CreateCallProxy_Invoke_Functor()
    {
        var testAddition = TestContext.CurrentContext.Random.Next();

        var serializers = new SerializerContainer();

        var proxy = ICallProxy.FromFunctor(arguments =>
        {
            var a = arguments["a"].AsInt32;
            var b = arguments["b"].AsInt32;
            return new Integer32Value(a + b + testAddition);
        });

        var proxyFunctor = CallProxyGenerator
            .For(typeof(SampleClass).GetMethod(nameof(SampleClass.Add))!)
            .CreateDelegate<Func<int, int, int>>(serializers, proxy);

        Assert.That(proxyFunctor, Is.Not.Null);
        Assert.That(proxyFunctor(1, 2), Is.EqualTo(3 + testAddition));
    }

    [Test]
    public async Task CreateCallProxy_Invoke_AsyncFunctor_Task()
    {
        var testAddition = TestContext.CurrentContext.Random.Next();

        var serializers = new SerializerContainer().UseProxyingSerializers();

        var proxy = ICallProxy.FromFunctor(async arguments =>
        {
            await Task.Yield();
            var a = arguments["a"].AsInt32;
            var b = arguments["b"].AsInt32;
            return new Integer32Value(a + b + testAddition);
        });

        var proxyFunctor = CallProxyGenerator
            .For(typeof(SampleClass).GetMethod(nameof(SampleClass.AddAsyncTask))!)
            .CreateDelegate<Func<int, int, Task<int>>>(serializers, proxy);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(proxyFunctor, Is.Not.Null);
            Assert.That(await proxyFunctor(1, 2), Is.EqualTo(3 + testAddition));
        }
    }

    [Test]
    public async Task CreateCallProxy_Invoke_AsyncFunctor_ValueTask()
    {
        var testAddition = TestContext.CurrentContext.Random.Next();

        var serializers = new SerializerContainer().UseProxyingSerializers();

        var proxy = ICallProxy.FromFunctor(async arguments =>
        {
            await Task.Yield();
            var a = arguments["a"].AsInt32;
            var b = arguments["b"].AsInt32;
            return new Integer32Value(a + b + testAddition);
        });

        var proxyFunctor = CallProxyGenerator
            .For(typeof(SampleClass).GetMethod(nameof(SampleClass.AddAsyncValueTask))!)
            .CreateDelegate<Func<int, int, ValueTask<int>>>(serializers, proxy);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(proxyFunctor, Is.Not.Null);
            Assert.That(await proxyFunctor(1, 2), Is.EqualTo(3 + testAddition));
        }
    }

    [Test]
    public void CreateCallProxy_Invoke_Action()
    {
        var serializers = new SerializerContainer();

        var proxy = ICallProxy.FromFunctor(_ => null);

        var proxyFunctor = CallProxyGenerator
            .For(typeof(SampleClass).GetMethod(nameof(SampleClass.DoNothing))!)
            .CreateDelegate<Action<int, int>>(serializers, proxy);

        Assert.That(proxyFunctor, Is.Not.Null);
        Assert.DoesNotThrow(() => proxyFunctor(1, 2));
    }

    [Test]
    public void CreateCallProxy_Invoke_AsyncAction_Task()
    {
        var serializers = new SerializerContainer().UseProxyingSerializers();

        var proxy = ICallProxy.FromFunctor(_ => null);

        var proxyFunctor = CallProxyGenerator
            .For(typeof(SampleClass).GetMethod(nameof(SampleClass.DoNothingAsyncTask))!)
            .CreateDelegate<Func<int, int, Task>>(serializers, proxy);

        Assert.That(proxyFunctor, Is.Not.Null);
        Assert.DoesNotThrowAsync(() => proxyFunctor(1, 2));
    }

    [Test]
    public void CreateCallProxy_Invoke_AsyncAction_ValueTask()
    {
        var serializers = new SerializerContainer().UseProxyingSerializers();

        var proxy = ICallProxy.FromFunctor(_ => null);

        var proxyFunctor = CallProxyGenerator
            .For(typeof(SampleClass).GetMethod(nameof(SampleClass.DoNothingAsyncValueTask))!)
            .CreateDelegate<Func<int, int, ValueTask>>(serializers, proxy);

        Assert.That(proxyFunctor, Is.Not.Null);
        Assert.DoesNotThrowAsync(async () => await proxyFunctor(1, 2));
    }
}