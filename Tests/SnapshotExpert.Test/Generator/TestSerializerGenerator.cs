using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Generator;

namespace SnapshotExpert.Test.Generator;

[TestFixture, TestOf(typeof(SerializerGenerator))]
public class TestSerializerGenerator
{
    public class StubBaseClass
    {
        private int _privateField;

        public int Field;

        public int Property { get; set; }

        public int PrivateField
        {
            get => _privateField;
            set => _privateField = value;
        }
    }

    public class StubChildClass : StubBaseClass
    {
        public int ChildField;

        public int ChildProperty { get; set; }
    }
    
    public struct StubStruct
    {
        public int Field;

        public int Property { get; set; }
    }

    [Test]
    public void GenerateSerializer_ForClass()
    {
        var context = new SerializerContainer();

        var serializer = context.GetSerializer<StubBaseClass>();
        
        Assert.That(serializer, Is.Not.Null);
    }
    
    [Test]
    public void GenerateSerializer_ForStruct()
    {
        var context = new SerializerContainer();

        var serializer = context.GetSerializer<StubBaseClass>();
        
        Assert.That(serializer, Is.Not.Null);
    }

    [Test]
    public void SaveSnapshot_Class()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<StubBaseClass>();
        
        var instance = new StubBaseClass
        {
            Field = TestContext.CurrentContext.Random.Next(),
            Property = TestContext.CurrentContext.Random.Next(),
            PrivateField = TestContext.CurrentContext.Random.Next()
        };

        var node = new SnapshotNode();
        
        serializer.SaveSnapshot(instance, node);

        Assert.Multiple(() =>
        {
            var value = (ObjectValue)node.Value!;
            Assert.That(value, Is.Not.Null);
            Assert.That((value["_privateField"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.PrivateField));
            Assert.That((value["Field"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.Field));
            Assert.That((value["Property"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.Property));
        });
    }
    
    [Test]
    public void LoadSnapshot_Class()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<StubBaseClass>();
        
        var privateFieldValue = TestContext.CurrentContext.Random.Next();
        var fieldValue = TestContext.CurrentContext.Random.Next();
        var propertyValue = TestContext.CurrentContext.Random.Next();
        

        var node = new SnapshotNode();
        node.AssignValue(new ObjectValue()
        {
            ["_privateField"] = new Integer32Value(privateFieldValue),
            ["Field"] = new Integer32Value(fieldValue),
            ["Property"] = new Integer32Value(propertyValue)
        });
        
        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);

        Assert.Multiple(() =>
        {
            Assert.That(restored.PrivateField, Is.EqualTo(privateFieldValue));
            Assert.That(restored.Field, Is.EqualTo(fieldValue));
            Assert.That(restored.Property, Is.EqualTo(propertyValue));
        });
    }
    
    [Test]
    public void SaveSnapshot_Struct()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<StubStruct>();
        
        var instance = new StubStruct
        {
            Field = TestContext.CurrentContext.Random.Next(),
            Property = TestContext.CurrentContext.Random.Next(),
        };

        var node = new SnapshotNode();
        
        serializer.SaveSnapshot(instance, node);

        Assert.Multiple(() =>
        {
            var value = (ObjectValue)node.Value!;
            Assert.That(value, Is.Not.Null);
            Assert.That((value["Field"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.Field));
            Assert.That((value["Property"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.Property));
        });
    }
    
    [Test]
    public void LoadSnapshot_Struct()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<StubStruct>();

        var fieldValue = TestContext.CurrentContext.Random.Next();
        var propertyValue = TestContext.CurrentContext.Random.Next();

        var node = new SnapshotNode();
        node.AssignValue(new ObjectValue()
        {
            ["Field"] = new Integer32Value(fieldValue),
            ["Property"] = new Integer32Value(propertyValue)
        });
        
        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);

        Assert.Multiple(() =>
        {
            Assert.That(restored.Field, Is.EqualTo(fieldValue));
            Assert.That(restored.Property, Is.EqualTo(propertyValue));
        });
    }
    
    [Test]
    public void SaveSnapshot_ChildClass()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<StubChildClass>();
        
        var instance = new StubChildClass()
        {
            ChildField = TestContext.CurrentContext.Random.Next(),
            ChildProperty = TestContext.CurrentContext.Random.Next(),
            Field = TestContext.CurrentContext.Random.Next(),
            Property = TestContext.CurrentContext.Random.Next(),
            PrivateField = TestContext.CurrentContext.Random.Next()
        };

        var node = new SnapshotNode();
        
        serializer.SaveSnapshot(instance, node);

        Assert.Multiple(() =>
        {
            var value = (ObjectValue)node.Value!;
            Assert.That(value, Is.Not.Null);
            Assert.That((value["_privateField"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.PrivateField));
            Assert.That((value["Field"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.Field));
            Assert.That((value["Property"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.Property));
            Assert.That((value["ChildField"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.ChildField));
            Assert.That((value["ChildProperty"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.ChildProperty));
        });
    }
    
    [Test]
    public void LoadSnapshot_ChildClass()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<StubChildClass>();
        
        var privateFieldValue = TestContext.CurrentContext.Random.Next();
        var fieldValue = TestContext.CurrentContext.Random.Next();
        var propertyValue = TestContext.CurrentContext.Random.Next();
        var childFieldValue = TestContext.CurrentContext.Random.Next();
        var childPropertyValue = TestContext.CurrentContext.Random.Next();

        var node = new SnapshotNode();
        node.AssignValue(new ObjectValue
        {
            ["_privateField"] = new Integer32Value(privateFieldValue),
            ["Field"] = new Integer32Value(fieldValue),
            ["Property"] = new Integer32Value(propertyValue),
            ["ChildField"] = new Integer32Value(childFieldValue),
            ["ChildProperty"] = new Integer32Value(childPropertyValue)
        });
        
        serializer.NewInstance(out var restored);
        serializer.LoadSnapshot(ref restored, node);

        Assert.Multiple(() =>
        {
            Assert.That(restored.PrivateField, Is.EqualTo(privateFieldValue));
            Assert.That(restored.Field, Is.EqualTo(fieldValue));
            Assert.That(restored.Property, Is.EqualTo(propertyValue));
            Assert.That(restored.ChildField, Is.EqualTo(childFieldValue));
            Assert.That(restored.ChildProperty, Is.EqualTo(childPropertyValue));
        });
    }
    
    public class StubClassWithTransientMembers
    {
        private int _privateField;

        public int Field;

        [Transient]
        public int Property { get; set; }

        public int PrivateField
        {
            get => _privateField;
            set => _privateField = value;
        }
    }
    
    [Test]
    public void SaveSnapshot_ClassWithTransientMembers()
    {
        var context = new SerializerContainer();

        var serializer = context.RequireSerializer<StubClassWithTransientMembers>();
        
        var instance = new StubClassWithTransientMembers()
        {
            Field = TestContext.CurrentContext.Random.Next(),
            Property = TestContext.CurrentContext.Random.Next(),
            PrivateField = TestContext.CurrentContext.Random.Next()
        };

        var node = new SnapshotNode();
        
        serializer.SaveSnapshot(instance, node);

        Assert.Multiple(() =>
        {
            var value = (ObjectValue)node.Value!;
            Assert.That(value, Is.Not.Null);
            Assert.That((value["_privateField"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.PrivateField));
            Assert.That((value["Field"] as Integer32Value)?.Value, 
                Is.EqualTo(instance.Field));
            Assert.That(value.GetDeclaredNode(nameof(StubClassWithTransientMembers.Property)), Is.Null);
        });
    }
}