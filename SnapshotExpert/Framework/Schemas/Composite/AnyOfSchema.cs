using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Framework.Schemas.Composite;

public class AnyOfSchema : SnapshotSchema
{
    public required IReadOnlyCollection<SnapshotSchema> Schemas { get; init; }

    protected override void OnGenerate(ObjectValue schema)
    {
        var array = schema.CreateNode("anyOf").AssignArray();
        foreach (var model in Schemas)
        {
            model.Generate(array.CreateNode().AssignObject());
        }
    }

    protected override bool OnValidate(SnapshotNode node) 
        => Schemas.Any(model => model.Validate(node));
}