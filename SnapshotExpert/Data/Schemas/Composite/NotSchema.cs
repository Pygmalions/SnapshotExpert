using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Composite;

public record NotSchema : SnapshotSchema
{
    public required SnapshotSchema Schema { get; init; }

    protected override void OnGenerate(ObjectValue schema)
    {
        Schema.Generate(schema.CreateNode("not").AssignValue(new ObjectValue()));
    }

    protected override bool OnValidate(SnapshotNode node) 
        => !Schema.Validate(node);
}