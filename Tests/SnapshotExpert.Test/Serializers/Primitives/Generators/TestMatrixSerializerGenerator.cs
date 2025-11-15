using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Serializers.Primitives.Generators;

namespace SnapshotExpert.Test.Serializers.Primitives.Generators;

[TestFixture, TestOf(typeof(MatrixSerializerGenerator))]
public class TestMatrixSerializerGenerator
{
    private SerializerContainer _context;

    [SetUp]
    public void Initialize()
    {
        _context = new SerializerContainer();
    }

    [Test]
    public void GenerateSerializerInstance()
    {
        var serializer = _context.RequireSerializer<int[,]>();
        Assert.That(serializer, Is.Not.Null);
    }

    [Test]
    public void SaveSnapshot()
    {
        var serializer = _context.RequireSerializer<int[,]>();

        var node = new SnapshotNode();
        var targetMatrix = new [,] { { 0, 0, 0 }, { 0, 0, 0 } };
        var arrays = new int[targetMatrix.GetLength(0)][];
        var arrayLength = targetMatrix.GetLength(1);
        for (var dimension = 0; dimension < 2; dimension++)
        {
            arrays[dimension] = new int[arrayLength];
            for (var index = 0; index < arrayLength; index++)
            {
                var number = TestContext.CurrentContext.Random.Next();
                arrays[dimension][index] = number;
                targetMatrix[dimension, index] = number;
            }
        }
        
        serializer.SaveSnapshot(targetMatrix, node);

        Assert.That(node.Value, Is.TypeOf<ArrayValue>());
        Assert.Multiple(() =>
        {
            var rootValue = (ArrayValue)node.Value!;
            Assert.That(rootValue.Nodes[0].Children
                    .Select(child => child.Value)
                    .OfType<Integer32Value>()
                    .Select(value => value.Value),
                Is.EquivalentTo(arrays[0]));
            Assert.That(rootValue.Nodes[1].Children
                    .Select(child => child.Value)
                    .OfType<Integer32Value>()
                    .Select(value => value.Value),
                Is.EquivalentTo(arrays[1]));
        });
    }
    
    [Test]
    public void LoadSnapshot_IntoNull()
    {
        var serializer = _context.RequireSerializer<int[,]>();

        var snapshotNode = new SnapshotNode();
        var matrixElement = snapshotNode.AssignArray();
        var arrays = new int[2][];
        var arrayLength = 3;
        for (var dimension = 0; dimension < 2; dimension++)
        {
            arrays[dimension] = new int[arrayLength];
            var arrayElement = matrixElement.CreateNode().AssignArray();
            for (var index = 0; index < arrayLength; index++)
            {
                var number = TestContext.CurrentContext.Random.Next();
                arrays[dimension][index] = number;
                arrayElement.CreateNode().AssignValue(number);
            }
        }
        
        serializer.NewInstance(out var deserializedMatrix);
        serializer.LoadSnapshot(ref deserializedMatrix, snapshotNode);
        
        Assert.Multiple(() =>
        {
            Assert.That(deserializedMatrix, Is.Not.Null);
            Assert.That(deserializedMatrix.GetLength(0), Is.EqualTo(2));
            for (var dimension = 0; dimension < arrays.Length; ++dimension)
            {
                Assert.That(deserializedMatrix.GetLength(1), 
                    Is.EqualTo(arrays[dimension].Length));
                for (var index = 0; index < arrays[dimension].Length; ++index)
                {
                    Assert.That(deserializedMatrix[dimension, index], Is.EqualTo(arrays[dimension][index]));
                }
            }
        });
    }
    
    [Test]
    public void LoadSnapshot_IntoDifferentShape()
    {
        var serializer = _context.RequireSerializer<int[,]>();

        var snapshotNode = new SnapshotNode();
        var matrixElement = snapshotNode.AssignArray();
        var arrays = new int[2][];
        var arrayLength = 3;
        for (var dimension = 0; dimension < 2; dimension++)
        {
            arrays[dimension] = new int[arrayLength];
            var arrayElement = matrixElement.CreateNode().AssignArray();
            for (var index = 0; index < arrayLength; index++)
            {
                var number = TestContext.CurrentContext.Random.Next();
                arrays[dimension][index] = number;
                arrayElement.CreateNode().AssignValue(number);
            }
        }
        
        var originalMatrix = new int[4, 5];
        var restoredMatrix = originalMatrix;
        serializer.LoadSnapshot(ref restoredMatrix, snapshotNode);
        
        Assert.Multiple(() =>
        {
            Assert.That(restoredMatrix, Is.Not.Null);
            Assert.That(restoredMatrix, Is.Not.SameAs(originalMatrix));
            Assert.That(restoredMatrix.GetLength(0), Is.EqualTo(2));
            for (var dimension = 0; dimension < arrays.Length; ++dimension)
            {
                Assert.That(restoredMatrix.GetLength(1), 
                    Is.EqualTo(arrays[dimension].Length));
                for (var index = 0; index < arrays[dimension].Length; ++index)
                {
                    Assert.That(restoredMatrix[dimension, index], Is.EqualTo(arrays[dimension][index]));
                }
            }
        });
    }
    
    [Test]
    public void LoadSnapshot_IntoSameShape()
    {
        var serializer = _context.RequireSerializer<int[,]>();

        var snapshotNode = new SnapshotNode();
        var matrixElement = snapshotNode.AssignArray();
        var arrays = new int[2][];
        var arrayLength = 3;
        for (var dimension = 0; dimension < 2; dimension++)
        {
            arrays[dimension] = new int[arrayLength];
            var arrayElement = matrixElement.CreateNode().AssignArray();
            for (var index = 0; index < arrayLength; index++)
            {
                var number = TestContext.CurrentContext.Random.Next();
                arrays[dimension][index] = number;
                arrayElement.CreateNode().AssignValue(number);
            }
        }
        
        var originalMatrix = new int[2, 3];
        var deserializedMatrix = originalMatrix;
        serializer.LoadSnapshot(ref deserializedMatrix, snapshotNode);
        
        Assert.Multiple(() =>
        {
            Assert.That(deserializedMatrix, Is.Not.Null);
            Assert.That(deserializedMatrix, Is.SameAs(originalMatrix));
            Assert.That(deserializedMatrix.GetLength(0), Is.EqualTo(2));
            for (var dimension = 0; dimension < arrays.Length; ++dimension)
            {
                Assert.That(deserializedMatrix.GetLength(1), 
                    Is.EqualTo(arrays[dimension].Length));
                for (var index = 0; index < arrays[dimension].Length; ++index)
                {
                    Assert.That(deserializedMatrix[dimension, index], Is.EqualTo(arrays[dimension][index]));
                }
            }
        });
    }
}