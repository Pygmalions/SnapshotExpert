using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Framework.Schemas.Composite;

public record AllOfSchema : SnapshotSchema
{
    public required IReadOnlyCollection<SnapshotSchema> Schemas { get; init; }
    
    protected override void OnGenerate(ObjectValue schema)
    {
        var array = schema.CreateNode("allOf").AssignArray();
        foreach (var model in Schemas)
        {
            model.Generate(array.CreateNode().AssignObject());
        }
    }

    protected override bool OnValidate(SnapshotNode node) 
        => Schemas.All(model => model.Validate(node));
}