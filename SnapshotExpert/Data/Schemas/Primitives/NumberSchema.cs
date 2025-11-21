using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Data.Schemas.Primitives;

public record NumberSchema() : PrimitiveSchema(JsonValueType.Number)
{
    public decimal? MinimumValue { get; init; }

    public decimal? MaximumValue { get; init; }

    public decimal? ExclusiveMinimumValue { get; init; }

    public decimal? ExclusiveMaximumValue { get; init; }

    /// <summary>
    /// The value should be a multiple of this number.
    /// </summary>
    public decimal? MultipleOfValue { get; init; }

    protected override void OnGenerate(ObjectValue schema)
    {
        if (MinimumValue != null)
            schema.CreateNode("minimum").BindValue(MinimumValue.Value);

        if (MaximumValue != null)
            schema.CreateNode("maximum").BindValue(MaximumValue.Value);

        if (ExclusiveMinimumValue != null)
            schema.CreateNode("exclusiveMinimum").BindValue(ExclusiveMinimumValue.Value);
        
        if (ExclusiveMaximumValue != null)
            schema.CreateNode("exclusiveMaximum").BindValue(ExclusiveMaximumValue.Value);

        if (MultipleOfValue != null)
            schema.CreateNode("multipleOf").BindValue(MultipleOfValue.Value);
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        decimal value;
        switch (node.Value)
        {
            case Integer32Value integer32:
                value = integer32.Value;
                break;
            case Integer64Value integer64:
                value = integer64.Value;
                break;
            case Float64Value floating64:
                value = new decimal(floating64.Value);
                break;
            case DecimalValue decimal128:
                value = decimal128.Value;
                break;
            default:
                return false;
        }

        if (value < MinimumValue)
            return false;
        if (value > MaximumValue)
            return false;
        if (value <= ExclusiveMinimumValue)
            return false;
        if (value >= ExclusiveMaximumValue)
            return false;
        if (MultipleOfValue != null && value % MultipleOfValue.Value != 0m)
            return false;
        return true;
    }
}