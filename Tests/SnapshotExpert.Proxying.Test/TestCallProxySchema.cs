using SnapshotExpert.Data.Schemas.Primitives;

namespace SnapshotExpert.Remoting.Test;

[TestFixture, TestOf(typeof(CallProxySchema))]
public class TestCallProxySchema
{
    private class SampleClass
    {
        public int Add(int a, int b, int c = 0) => a + b;
    }

    [Test]
    public void GenerateSchema()
    {
        var serializer = new SerializerContainer();

        var schema = CallProxySchema.For(typeof(SampleClass).GetMethod(nameof(SampleClass.Add))!, serializer);

        Assert.That(schema, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.RequiredProperties, Is.Not.Null);
            Assert.That(schema.OptionalProperties, Is.Not.Null);
        }

        Assert.That(schema.RequiredProperties.Count, Is.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.RequiredProperties["a"], Is.TypeOf<IntegerSchema>());
            Assert.That(schema.RequiredProperties["b"], Is.TypeOf<IntegerSchema>());
            Assert.That(schema.RequiredProperties.ContainsKey("c"), Is.False);
            Assert.That(schema.OptionalProperties["c"], Is.TypeOf<IntegerSchema>());
        }
    }
}