using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public class TupleSchema() : PrimitiveSchema(JsonValueType.Array)
{
    public required IReadOnlyCollection<SnapshotSchema> Items { get; init; }
    
    /// <summary>
    /// Schema for additional items.
    /// If null, additional items are not allowed.
    /// Default value is null.
    /// </summary>
    public SnapshotSchema? AdditionalItems { get; init; } = null;
    
    protected override void OnGenerate(ObjectValue schema)
    {
        var items = schema.CreateNode("items").AssignArray();
        foreach (var item in Items)
            item.Generate(items.CreateNode().AssignObject());

        var additionalItems = schema.CreateNode("additionalItems");
        if (AdditionalItems == null)
            additionalItems.AssignValue(false);
        else
            AdditionalItems.Generate(additionalItems.AssignObject());
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        if (node.Value is not ArrayValue array)
            return false;
        using var arrayEnumerator = array.Nodes.GetEnumerator();
        using var schemaEnumerator = Items.GetEnumerator();
        while (schemaEnumerator.MoveNext())
        {
            if (!arrayEnumerator.MoveNext())
                return false;
            if (!schemaEnumerator.Current.Validate(arrayEnumerator.Current!))
                return false;
        }
        if (AdditionalItems == null)
            return !arrayEnumerator.MoveNext();
        while (arrayEnumerator.MoveNext())
        {
            if (!AdditionalItems.Validate(arrayEnumerator.Current!))
                return false;
        }
        return true;
    }
}