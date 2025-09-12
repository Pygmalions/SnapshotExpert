using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public record ArraySchema() : PrimitiveSchema(JsonValueType.Array)
{
    /// <summary>
    /// Schema for items in this array.
    /// If null, any item is allowed.
    /// </summary>
    public SnapshotSchema? Items { get; init; } = null;

    public int? MinCount { get; init; } = null;

    public int? MaxCount { get; init; } = null;
    
    /// <summary>
    /// If true, duplicate items are not allowed.
    /// It is false by default.
    /// </summary>
    public bool RequiringUniqueItems { get; init; } = false;
    
    protected override void OnGenerate(ObjectValue schema)
    {
        Items?.Generate(schema.CreateNode("items").AssignObject());

        if (MinCount != null)
            schema.CreateNode("minItems").AssignValue(MinCount.Value);
        
        if (MaxCount != null)
            schema.CreateNode("maxItems").AssignValue(MaxCount.Value);

        if (RequiringUniqueItems)
            schema.CreateNode("uniqueItems").AssignValue(RequiringUniqueItems);
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        if (node.Value is not ArrayValue array)
            return false;
        if (MinCount != null && array.Count < MinCount)
            return false;
        if (MaxCount != null && array.Count > MaxCount)
            return false;
        if (RequiringUniqueItems && 
            array.Nodes
                .Distinct(SnapshotNodeContentEqualityComparer.Instance)
                .Count() != array.Count)
            return false;
        return Items == null ? IsNullable : array.Nodes.All(elementNode => Items.Validate(elementNode));
    }
}