using InjectionExpert;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Remoting.Generators;

namespace SnapshotExpert.Remoting.Test.Generators;

[TestFixture]
public class TestCallServerGenerator
{
    public class SampleClass
    {
        public int Add(int a, int b) => a + b;

        public int Number;

        public int GetNumber() => Number;

        public int GetSum(int a, int b = 1) => a + b;
    }

    [Test]
    public void GenerateCallServer_NotNull()
    {
        var serverType = CallServerGenerator.For(typeof(SampleClass));

        var context = new SerializerContainer();

        var instance = (ICallServer)context.AsInjections().NewObject(serverType);

        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void HandleCall_WithoutArguments()
    {
        var serverType = CallServerGenerator.For(typeof(SampleClass));

        var context = new SerializerContainer();

        var instance = (ICallServer)context.AsInjections().NewObject(serverType);

        Assert.That(instance, Is.Not.Null);

        var testInstance = new SampleClass
        {
            Number = TestContext.CurrentContext.Random.Next()
        };

        var result = instance.HandleCall(
            testInstance,
            typeof(SampleClass).GetMethod(nameof(SampleClass.GetNumber))!.MetadataToken,
            new ObjectValue()) as Integer32Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(testInstance.Number));
    }

    [Test]
    public void HandleCall_WithArguments()
    {
        var serverType = CallServerGenerator.For(typeof(SampleClass));

        var context = new SerializerContainer();

        var instance = (ICallServer)context.AsInjections().NewObject(serverType);

        Assert.That(instance, Is.Not.Null);

        var testInstance = new SampleClass();
        var testNumberA = TestContext.CurrentContext.Random.Next();
        var testNumberB = TestContext.CurrentContext.Random.Next();

        var result = instance.HandleCall(
            testInstance,
            typeof(SampleClass).GetMethod(nameof(SampleClass.Add))!.MetadataToken,
            new ObjectValue(new Dictionary<string, SnapshotValue>
            {
                { "a", new Integer32Value(testNumberA) },
                { "b", new Integer32Value(testNumberB) },
            })) as Integer32Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(testNumberA + testNumberB));
    }
    
    [Test]
    public void HandleCall_WithArguments_WithDefaultArgument()
    {
        var serverType = CallServerGenerator.For(typeof(SampleClass));

        var context = new SerializerContainer();

        var instance = (ICallServer)context.AsInjections().NewObject(serverType);

        Assert.That(instance, Is.Not.Null);

        var testInstance = new SampleClass();
        var testNumberA = TestContext.CurrentContext.Random.Next();

        var result = instance.HandleCall(
            testInstance,
            typeof(SampleClass).GetMethod(nameof(SampleClass.GetSum))!.MetadataToken,
            new ObjectValue(new Dictionary<string, SnapshotValue>
            {
                { "a", new Integer32Value(testNumberA) },
            })) as Integer32Value;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(testNumberA + 1));
    }
}