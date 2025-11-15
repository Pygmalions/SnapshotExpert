namespace SnapshotExpert.Data.Schemas;

/// <summary>
/// JSON value types. 'Null' is not included because it is handled by
/// <see cref="PrimitiveSchema.IsNullable"/> property.
/// </summary>
public enum JsonValueType
{
    Object,
    Array,
    String,
    Number,
    Integer,
    Boolean
}

/// <summary>
/// Names of JSON value types.
/// </summary>
/// <seealso href="https://json-schema.org/understanding-json-schema/reference/type"/>
public static class JsonValueTypeNames
{
    public const string Null = "null";
    public const string Object = "object";
    public const string Array = "array";
    public const string Boolean = "boolean";
    public const string String = "string";
    public const string Integer = "integer";
    public const string Number = "number";
}

public static class SchemaTypeExtensions
{
    public static string ToTypeName(this JsonValueType type)
    {
        return type switch
        {
            JsonValueType.Object => JsonValueTypeNames.Object,
            JsonValueType.Array => JsonValueTypeNames.Array,
            JsonValueType.String => JsonValueTypeNames.String,
            JsonValueType.Number => JsonValueTypeNames.Number,
            JsonValueType.Integer => JsonValueTypeNames.Integer,
            JsonValueType.Boolean => JsonValueTypeNames.Boolean,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}