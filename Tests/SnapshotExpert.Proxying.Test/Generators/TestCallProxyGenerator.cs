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
    }

    [Test]
    public void CreateCallProxy_Invoke()
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
}