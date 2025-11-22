using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Primitives;

/// <summary>
/// This empty schema accepts any value.
/// </summary>
public record EmptySchema : SnapshotSchema
{
    protected override void OnGenerate(ObjectValue schema)
    {
    }

    protected override bool OnValidate(SnapshotNode node)
        => true;


    public override bool Validate(SnapshotNode node)
        => true;
}