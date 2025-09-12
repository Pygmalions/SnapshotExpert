using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public record TypesSchema : PrimitiveSchema
{
    public TypesSchema(params Span<JsonValueType> types) : base(types)
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