using SnapshotExpert.Framework.Values;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public class NullSchema : PrimitiveSchema
{
    public NullSchema()
    {
        IsNullable = true;
    }
    
    protected override void OnGenerate(ObjectValue schema)
    {}

    protected override bool OnValidate(SnapshotNode node)
        => node.Value is NullValue;
}