using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public record class IntegerSchema() : PrimitiveSchema(JsonValueType.Integer)
{
    public long? Minimum { get; init; } = null;

    public long? Maximum { get; init; } = null;

    public long? ExclusiveMinimum { get; init; } = null;
    
    public long? ExclusiveMaximum { get; init; } = null;
    
    protected override void OnGenerate(ObjectValue schema)
    {
        if (Minimum != null)
            schema.CreateNode("minimum").AssignValue(Minimum.Value);

        if (Maximum != null)
            schema.CreateNode("maximum").AssignValue(Maximum.Value);

        if (ExclusiveMinimum != null)
            schema.CreateNode("exclusiveMinimum").AssignValue(ExclusiveMinimum.Value);

        if (ExclusiveMaximum != null)
            schema.CreateNode("exclusiveMaximum").AssignValue(ExclusiveMaximum.Value);
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        long value;
        switch (node.Value)
        {
            case Integer32Value integer32:
                value = integer32.Value;
                break;
            case Integer64Value integer64:
                value = integer64.Value;
                break;
            default:
                return false;
        }
        if (value < Minimum)
            return false;
        if (value > Maximum)
            return false;
        if (value <= ExclusiveMinimum)
            return false;
        if (value >= ExclusiveMaximum)
            return false;
        return true;
    }
}