using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Primitives;

public record ArraySchema() : PrimitiveSchema(JsonValueType.Array)
{
    /// <summary>
    /// Schema for items in this array.
    /// If null, any item is allowed.
    /// </summary>
    public SnapshotSchema? Items { get; init; }

    public int? MinCount { get; init; }

    public int? MaxCount { get; init; }
    
    /// <summary>
    /// If true, duplicate items are not allowed.
    /// It is false by default.
    /// </summary>
    public bool RequiringUniqueItems { get; init; }
    
    protected override void OnGenerate(ObjectValue schema)
    {
        Items?.Generate(schema.CreateNode("items").AssignValue(new ObjectValue()));

        if (MinCount != null)
            schema.CreateNode("minItems").BindValue(MinCount.Value);
        
        if (MaxCount != null)
            schema.CreateNode("maxItems").BindValue(MaxCount.Value);

        if (RequiringUniqueItems)
            schema.CreateNode("uniqueItems").BindValue(RequiringUniqueItems);
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
            array.DeclaredNodes
                .Distinct(SnapshotNodeContentEqualityComparer.Instance)
                .Count() != array.Count)
            return false;
        return Items == null ? IsNullable : array.DeclaredNodes.All(elementNode => Items.Validate(elementNode));
    }
}