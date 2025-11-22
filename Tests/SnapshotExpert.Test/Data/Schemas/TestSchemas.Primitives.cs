using System.Text.RegularExpressions;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Test.Data.Schemas;

[TestFixture]
public class TestSchemasPrimitives
{
    // Helper to build a node with a value
    private static SnapshotNode Node(SnapshotValue? value)
    {
        var node = new SnapshotNode();
        if (value != null)
            node.AssignValue(value);
        return node;
    }

    [Test]
    public void BooleanSchema_Validate()
    {
        var schema = new BooleanSchema();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(new BooleanValue(true))), Is.True);
            Assert.That(schema.Validate(Node(new BooleanValue(false))), Is.True);
            Assert.That(schema.Validate(Node(new StringValue("x"))), Is.False);
            Assert.That(schema.Validate(Node(new Integer32Value(1))), Is.False);
            Assert.That(schema.Validate(Node(new NullValue())), Is.False);
        }

        // Smoke: generation should produce a BSON document with a 'type' field
        var value = schema.DumpToSnapshotValue();
        Assert.That(value.Contains("type"), Is.True);
    }

    [Test]
    public void IntegerSchema_Validate_Bounds()
    {
        var schema = new IntegerSchema
        {
            Minimum = 0,
            Maximum = 10,
            ExclusiveMinimum = -1,
            ExclusiveMaximum = 11
        };

        using (Assert.EnterMultipleScope())
        {
            // Within bounds
            Assert.That(schema.Validate(Node(new Integer32Value(0))), Is.True);
            Assert.That(schema.Validate(Node(new Integer64Value(10))), Is.True);

            // Violations
            Assert.That(schema.Validate(Node(new Integer32Value(-1))), Is.False);
            Assert.That(schema.Validate(Node(new Integer64Value(11))), Is.False);

            // Wrong type
            Assert.That(schema.Validate(Node(new Float64Value(1.0))), Is.False);
            Assert.That(schema.Validate(Node(new StringValue("1"))), Is.False);
        }

        // Smoke: generation
        var value = schema.DumpToSnapshotValue();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.Contains("type"), Is.True);
            Assert.That(value.Contains("minimum"), Is.True);
            Assert.That(value.Contains("maximum"), Is.True);
            Assert.That(value.Contains("exclusiveMinimum"), Is.True);
            Assert.That(value.Contains("exclusiveMaximum"), Is.True);
        }
    }

    [Test]
    public void NumberSchema_Validate_MinMax_MultipleOf()
    {
        var schema = new NumberSchema
        {
            MinimumValue = 0m,
            MaximumValue = 5m,
            ExclusiveMinimumValue = -1m,
            ExclusiveMaximumValue = 6m,
            MultipleOfValue = 0.5m
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(new Integer32Value(0))), Is.True);
            Assert.That(schema.Validate(Node(new Integer64Value(4))), Is.True);
            Assert.That(schema.Validate(Node(new Float64Value(3.0))), Is.True);
            Assert.That(schema.Validate(Node(new DecimalValue(4.5m))), Is.True);

            // MultipleOf violation
            Assert.That(schema.Validate(Node(new DecimalValue(4.4m))), Is.False);

            // Range violations
            Assert.That(schema.Validate(Node(new DecimalValue(-1m))), Is.False);
            Assert.That(schema.Validate(Node(new DecimalValue(6m))), Is.False);

            // Wrong type
            Assert.That(schema.Validate(Node(new StringValue("1"))), Is.False);
        }

        // Smoke: generation
        var value = schema.DumpToSnapshotValue();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.Contains("type"), Is.True);
            Assert.That(value.Contains("minimum"), Is.True);
            Assert.That(value.Contains("maximum"), Is.True);
            Assert.That(value.Contains("exclusiveMinimum"), Is.True);
            Assert.That(value.Contains("exclusiveMaximum"), Is.True);
            Assert.That(value.Contains("multipleOf"), Is.True);
        }
    }

    [Test]
    public void StringSchema_Validate_Length_And_Pattern()
    {
        var schema = new StringSchema
        {
            MinLength = 2,
            MaxLength = 4,
            Pattern = new Regex("^[a-z]+$"),
            Format = StringSchema.BuiltinFormats.Email,
            ContentEncoding = StringSchema.ContentEncodingType.Base64,
            ContentMediaType = "text/plain"
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(new StringValue("ab"))), Is.True);
            Assert.That(schema.Validate(Node(new StringValue("abcd"))), Is.True);
            Assert.That(schema.Validate(Node(new StringValue("a"))), Is.False); // too short
            Assert.That(schema.Validate(Node(new StringValue("abcde"))), Is.False); // too long
            Assert.That(schema.Validate(Node(new StringValue("AB"))), Is.False); // pattern mismatch
            Assert.That(schema.Validate(Node(new Integer32Value(1))), Is.False); // wrong type
        }

        // Smoke: generation
        var value = schema.DumpToSnapshotValue();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.Contains("type"), Is.True);
            Assert.That(value.Contains("minLength"), Is.True);
            Assert.That(value.Contains("maxLength"), Is.True);
            Assert.That(value.Contains("pattern"), Is.True);
            Assert.That(value.Contains("format"), Is.True);
            Assert.That(value.Contains("contentEncoding"), Is.True);
            Assert.That(value.Contains("contentMediaType"), Is.True);
        }
    }

    [Test]
    public void NullSchema_Validate()
    {
        var schema = new NullSchema();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(new NullValue())), Is.True);
            Assert.That(schema.Validate(Node(new StringValue("x"))), Is.False);
        }

        // Smoke: generation shouldn't throw
        Assert.DoesNotThrow(() => schema.DumpToBsonDocument());
    }

    [Test]
    public void EmptySchema_Validate_Allows_Any()
    {
        var schema = new EmptySchema();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(new NullValue())), Is.True);
            Assert.That(schema.Validate(Node(new StringValue("x"))), Is.True);
            Assert.That(schema.Validate(Node(new Integer32Value(1))), Is.True);
            Assert.That(schema.Validate(Node(new Float64Value(1.2))), Is.True);
            Assert.That(schema.Validate(Node(new DecimalValue(1.2m))), Is.True);
            Assert.That(schema.Validate(Node(new BooleanValue(true))), Is.True);
            Assert.That(schema.Validate(Node(new ArrayValue())), Is.True);
            Assert.That(schema.Validate(Node(new ObjectValue())), Is.True);
        }

        // Smoke: generation shouldn't throw
        Assert.DoesNotThrow(() => schema.DumpToBsonDocument());
    }

    [Test]
    public void ArraySchema_Validate_Items_Unique_MinMax()
    {
        var itemSchema = new IntegerSchema { Minimum = 0, Maximum = 3 };
        var schema = new ArraySchema
        {
            Items = itemSchema,
            MinCount = 2,
            MaxCount = 3,
            RequiringUniqueItems = true
        };

        var validArray = new ArrayValue
        {
            new Integer32Value(0),
            new Integer64Value(3)
        };
        var tooShort = new ArrayValue { new Integer32Value(1) };
        var tooLong = new ArrayValue
        {
            new Integer32Value(0), new Integer32Value(1), new Integer32Value(2), new Integer32Value(3),
        };
        var duplicates = new ArrayValue { new Integer32Value(1), new Integer32Value(1) };
        var wrongItem = new ArrayValue { new StringValue("x"), new Integer32Value(1) };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(validArray)), Is.True);
            Assert.That(schema.Validate(Node(tooShort)), Is.False);
            Assert.That(schema.Validate(Node(tooLong)), Is.False);
            Assert.That(schema.Validate(Node(duplicates)), Is.False);
            Assert.That(schema.Validate(Node(wrongItem)), Is.False);
            Assert.That(schema.Validate(Node(new StringValue("not array"))), Is.False);
        }

        // Smoke: generation should include array constraints when set
        var value = schema.DumpToSnapshotValue();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.Contains("type"), Is.True);
            Assert.That(value.Contains("minItems"), Is.True);
            Assert.That(value.Contains("maxItems"), Is.True);
            Assert.That(value.Contains("uniqueItems"), Is.True);
            Assert.That(value.Contains("items"), Is.True);
        }
    }

    [Test]
    public void ObjectSchema_Validate_Required_Optional_Additional()
    {
        var schema = new ObjectSchema
        {
            RequiredProperties = new OrderedDictionary<string, SnapshotSchema>
            {
                ["id"] = new IntegerSchema()
            },
            OptionalProperties = new OrderedDictionary<string, SnapshotSchema>
            {
                ["name"] = new StringSchema { MinLength = 1 }
            },
            AdditionalProperties = new BooleanSchema()
        };

        var valid = new ObjectValue
        {
            ["id"] = new Integer32Value(1),
            ["name"] = new StringValue("a"),
            ["flag"] = new BooleanValue(true)
        };
        var missingRequired = new ObjectValue
        {
            ["name"] = new StringValue("a")
        };
        var wrongOptional = new ObjectValue
        {
            ["id"] = new Integer32Value(1),
            ["name"] = new Integer32Value(1)
        };
        var wrongAdditional = new ObjectValue
        {
            ["id"] = new Integer32Value(1),
            ["extra"] = new StringValue("x")
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(valid)), Is.True);
            Assert.That(schema.Validate(Node(missingRequired)), Is.False);
            Assert.That(schema.Validate(Node(wrongOptional)), Is.False);
            Assert.That(schema.Validate(Node(wrongAdditional)), Is.False);
        }

        // Smoke: generation should include properties/required/additionalProperties
        var bson = schema.DumpToBsonDocument();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(bson.Contains("type"), Is.True);
            Assert.That(bson.Contains("properties"), Is.True);
            Assert.That(bson.Contains("required"), Is.True);
            Assert.That(bson.Contains("additionalProperties"), Is.True);
        }
    }

    [Test]
    public void AnyValueOfTypesSchema_Validate_SelectedTypes()
    {
        var schema = new AnyValueOfTypesSchema(JsonValueType.String, JsonValueType.Integer, JsonValueType.Boolean);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(new StringValue("x"))), Is.True);
            Assert.That(schema.Validate(Node(new Integer32Value(1))), Is.True);
            Assert.That(schema.Validate(Node(new Integer64Value(1))), Is.True);
            Assert.That(schema.Validate(Node(new BooleanValue(true))), Is.True);
            Assert.That(schema.Validate(Node(new Float64Value(1.0))), Is.False);
            Assert.That(schema.Validate(Node(new ArrayValue())), Is.False);
            Assert.That(schema.Validate(Node(new ObjectValue())), Is.False);
        }
    }

    [Test]
    public void BinarySchema_Validate_And_Generate()
    {
        var schema = new BinarySchema();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema.Validate(Node(new BinaryValue(new byte[] { 1, 2, 3 }))), Is.True);
            Assert.That(schema.Validate(Node(new StringValue("x"))), Is.False);
        }

        // Generated schema should be an object with $binary property
        var value = schema.DumpToSnapshotValue();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.Contains("type"), Is.True);
            Assert.That(value.Contains("properties"), Is.True);
        }

        var props = value["properties"].AsObject;
        Assert.That(props.Contains("$binary"), Is.True);
    }
}