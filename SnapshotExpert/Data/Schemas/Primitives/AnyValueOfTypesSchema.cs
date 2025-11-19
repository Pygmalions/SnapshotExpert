using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data.Schemas.Primitives;

/// <summary>
/// This schema accepts any value of the specified types.
/// </summary>
public record AnyValueOfTypesSchema : PrimitiveSchema
{
    public AnyValueOfTypesSchema(params Span<JsonValueType> types) : base(types)
    {
    }
    
    protected override void OnGenerate(ObjectValue schema)
    {
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        return node.Value switch
        {
            BooleanValue => AllowedTypes.Contains(JsonValueType.Boolean),
            StringValue => AllowedTypes.Contains(JsonValueType.String),
            Integer32Value or Integer64Value => AllowedTypes.Contains(JsonValueType.Integer) ||
                                                AllowedTypes.Contains(JsonValueType.Number),
            Float64Value or DecimalValue => AllowedTypes.Contains(JsonValueType.Number),
            NullValue => IsNullable,
            ArrayValue => AllowedTypes.Contains(JsonValueType.Array),
            ObjectValue => AllowedTypes.Contains(JsonValueType.Object),
            _ => false
        };
    }
}