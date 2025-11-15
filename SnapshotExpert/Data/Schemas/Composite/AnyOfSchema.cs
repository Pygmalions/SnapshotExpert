using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Composite;

public record AnyOfSchema : SnapshotSchema
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