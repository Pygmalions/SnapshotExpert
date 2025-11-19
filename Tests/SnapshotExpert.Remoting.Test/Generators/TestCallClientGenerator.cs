using InjectionExpert;
using InjectionExpert.Injectors;
using InjectionExpert.Utilities;
using Moq;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Remoting.Generators;

namespace SnapshotExpert.Remoting.Test.Generators;

[TestFixture, TestOf(typeof(CallClientGenerator))]
public class TestCallClientGenerator
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class SampleClass
    {
        public int Add(int a, int b) => a + b;

        public virtual int Number { get; }
        
        public virtual int VirtualAdd(int a, int b) => a + b;
    }
    
    public class SampleClassWithAsyncMethods
    {
        public async Task<int> Add(int a, int b) => a + b;
        
        public virtual async Task<int> VirtualAdd(int a, int b) => a + b;
    }
    
    [Test]
    public void GenerateCallClient_NotNull()
    {
        var clientType = CallClientGenerator.For(typeof(SampleClass));

        var serializers = new SerializerContainer();
        var injections = new InjectionContainer();
        
        var transporter = new Mock<ICallTransporter>();
        transporter
            .Setup(target =>
                target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()))
            .Returns(() => ValueTask.FromResult((SnapshotValue?)new Integer32Value(10)));

        injections.AddSingleton(transporter.Object);
        
        var injector = IInjectionProvider.FromMultiple(
            injections, serializers.AsInjections());
        
        var instance = injector.NewObject(clientType);

        Assert.That(instance, Is.Not.Null);
        Assert.That(instance as ICallClient, Is.Not.Null);
        Assert.That(instance as SampleClass, Is.Not.Null);
    }

    [Test]
    public void Call_Original_NonVirtual()
    {
        var clientType = CallClientGenerator.For(typeof(SampleClass));
    
        var instance = new SampleClass();
        
        var testNumberA = TestContext.CurrentContext.Random.Next();
        var testNumberB = TestContext.CurrentContext.Random.Next();
    
        Assert.That(instance.Add(testNumberA, testNumberB), 
            Is.EqualTo(testNumberA + testNumberB));
    }
    
    [Test]
    public void Call_Proxy_NonVirtual()
    {
        var serializers = new SerializerContainer();
        
        var testNumber = TestContext.CurrentContext.Random.Next();
        
        var transporter = new Mock<ICallTransporter>();
        transporter
            .Setup(target =>
                target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()))
            .Returns(() => ValueTask.FromResult((SnapshotValue?)new Integer32Value(testNumber)));
        
        var instance = CallClientGenerator.New<SampleClass>(
            serializers.AsInjections(), transporter.Object);

        Assert.That(instance.Add(1, 1), Is.EqualTo(testNumber));
        transporter.Verify(
            target => target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()),
            Times.Once);
    }
    
    [Test]
    public void Call_Proxy_Virtual()
    {
        var serializers = new SerializerContainer();
        
        var testNumber = TestContext.CurrentContext.Random.Next();
        
        var transporter = new Mock<ICallTransporter>();
        transporter
            .Setup(target =>
                target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()))
            .Returns(() => ValueTask.FromResult((SnapshotValue?)new Integer32Value(testNumber)));

        var instance = CallClientGenerator.New<SampleClass>(
            serializers.AsInjections(), transporter.Object);
    
        Assert.That(instance.VirtualAdd(1, 1), Is.EqualTo(testNumber));
        
        transporter.Verify(
            target => target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()),
            Times.Once);
    }
    
    [Test]
    public async Task Call_Async_Proxy_NonVirtual()
    {
        var serializers = new SerializerContainer()
            .UseRemotingSerializers();
        
        var testNumber = TestContext.CurrentContext.Random.Next();
        
        var transporter = new Mock<ICallTransporter>();
        transporter
            .Setup(target =>
                target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()))
            .Returns(() => ValueTask.FromResult((SnapshotValue?)new Integer32Value(testNumber)));
        
        var instance = CallClientGenerator.New<SampleClassWithAsyncMethods>(
            serializers.AsInjections(), transporter.Object);

        Assert.That(await instance.Add(1, 1), Is.EqualTo(testNumber));
        transporter.Verify(
            target => target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()),
            Times.Once);
    }
    
    [Test]
    public async Task Call_Async_Proxy_Virtual()
    {
        var serializers = new SerializerContainer()
            .UseRemotingSerializers();
        
        var testNumber = TestContext.CurrentContext.Random.Next();
        
        var transporter = new Mock<ICallTransporter>();
        transporter
            .Setup(target =>
                target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()))
            .Returns(() => ValueTask.FromResult((SnapshotValue?)new Integer32Value(testNumber)));

        var instance = CallClientGenerator.New<SampleClassWithAsyncMethods>(
            serializers.AsInjections(), transporter.Object);
    
        Assert.That(await instance.VirtualAdd(1, 1), Is.EqualTo(testNumber));
        
        transporter.Verify(
            target => target.Call(It.IsAny<int>(), It.IsAny<ObjectValue>()),
            Times.Once);
    }
}