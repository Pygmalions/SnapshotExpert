using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Framework.Schemas.Composite;

public class NotSchema : SnapshotSchema
{
    public required SnapshotSchema Schema { get; init; }

    protected override void OnGenerate(ObjectValue schema)
    {
        Schema.Generate(schema.CreateNode("not").AssignObject());
    }

    protected override bool OnValidate(SnapshotNode node) 
        => !Schema.Validate(node);
}