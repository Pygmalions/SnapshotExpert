using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data.Schemas.Primitives;

public record BooleanSchema() : PrimitiveSchema(JsonValueType.Boolean)
{
    protected override void OnGenerate(ObjectValue schema)
    {}

    protected override bool OnValidate(SnapshotNode node)
        => node.Value is BooleanValue;
}