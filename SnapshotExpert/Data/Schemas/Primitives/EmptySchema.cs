using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Primitives;

/// <summary>
/// This empty schema accepts any value.
/// </summary>
public record EmptySchema : PrimitiveSchema
{
    protected override void OnGenerate(ObjectValue schema)
    {
    }

    protected override bool OnValidate(SnapshotNode node)
        => true;
}