using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public class BooleanSchema() : PrimitiveSchema(JsonValueType.Boolean)
{
    protected override void OnGenerate(ObjectValue schema)
    {}

    protected override bool OnValidate(SnapshotNode node)
        => node.Value is BooleanValue;
}