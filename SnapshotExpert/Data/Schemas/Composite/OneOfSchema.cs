using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Composite;

public record OneOfSchema : SnapshotSchema
{
    public required IReadOnlyCollection<SnapshotSchema> Schemas { get; init; }
    
    protected override void OnGenerate(ObjectValue schema)
    {
        var array = schema.CreateNode("oneOf").AssignValue(new ArrayValue());
        foreach (var model in Schemas)
            model.Generate(array.CreateNode().AssignValue(new ObjectValue()));
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