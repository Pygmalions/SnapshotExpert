using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Primitives;

public record TupleSchema() : PrimitiveSchema(JsonValueType.Array)
{
    public required IReadOnlyCollection<SnapshotSchema?> Items { get; init; }
    
    /// <summary>
    /// Schema for additional items.
    /// If null, additional items are not allowed.
    /// The default value is null.
    /// </summary>
    public SnapshotSchema? AdditionalItems { get; init; } = null;
    
    protected override void OnGenerate(ObjectValue schema)
    {
        var items = schema.CreateNode("items").AssignValue(new ArrayValue());
        foreach (var item in Items)
            item?.Generate(items.CreateNode().AssignValue(new ObjectValue()));

        var additionalItems = schema.CreateNode("additionalItems");
        if (AdditionalItems == null)
            additionalItems.BindValue(false);
        else
            AdditionalItems.Generate(additionalItems.AssignValue(new ObjectValue()));
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        if (node.Value is not ArrayValue array)
            return false;
        using var arrayEnumerator = array.DeclaredNodes.GetEnumerator();
        using var schemaEnumerator = Items.GetEnumerator();
        while (schemaEnumerator.MoveNext())
        {
            var schema = schemaEnumerator.Current;
            if (schema is null)
                continue;
            if (!arrayEnumerator.MoveNext())
                return false;
            if (schema.Validate(arrayEnumerator.Current))
                return false;
        }
        if (AdditionalItems == null)
            return !arrayEnumerator.MoveNext();
        while (arrayEnumerator.MoveNext())
        {
            if (!AdditionalItems.Validate(arrayEnumerator.Current))
                return false;
        }
        return true;
    }
}