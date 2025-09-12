using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Framework.Schemas.Composite;

public record OneOfSchema : SnapshotSchema
{
    public required IReadOnlyCollection<SnapshotSchema> Schemas { get; init; }
    
    protected override void OnGenerate(ObjectValue schema)
    {
        var array = schema.CreateNode("oneOf").AssignArray();
        foreach (var model in Schemas)
            model.Generate(array.CreateNode().AssignObject());
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        var matched = false;
        foreach (var model in Schemas)
        {
            if (!model.Validate(node))
                continue;
            if (matched)
                return false;
            matched = true;
        }
        return matched;
    }
}