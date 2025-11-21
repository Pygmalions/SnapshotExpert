using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Composite;

public record AllOfSchema : SnapshotSchema
{
    public required IReadOnlyCollection<SnapshotSchema> Schemas { get; init; }
    
    protected override void OnGenerate(ObjectValue schema)
    {
        var array = schema.CreateNode("allOf").AssignValue(new ArrayValue());
        foreach (var model in Schemas)
        {
            model.Generate(array.CreateNode().AssignValue(new ObjectValue()));
        }
    }

    protected override bool OnValidate(SnapshotNode node) 
        => Schemas.All(model => model.Validate(node));
}