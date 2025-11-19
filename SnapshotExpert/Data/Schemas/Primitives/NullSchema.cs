using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data.Schemas.Primitives;

/// <summary>
/// This schema only accepts null.
/// </summary>
public record NullSchema : PrimitiveSchema
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