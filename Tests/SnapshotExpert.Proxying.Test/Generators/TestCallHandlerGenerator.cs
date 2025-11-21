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
    }

    [Test]
    public void CreateCallHandler_Invoke()
    {
        var instance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next(0, 100)
        };

        var serializers = new SerializerContainer();

        var handler = CallHandlerGenerator.For(serializers, instance.Add);

        var result = handler.HandleCall(new ObjectValue(new OrderedDictionary<string, SnapshotValue>()
            {
                { "a", new Integer32Value(1) },
                { "b", new Integer32Value(2) }
            }
        )) as Integer32Value;
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(instance.Add(1, 2)));
    }
}