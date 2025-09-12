using System.Text.RegularExpressions;
using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Framework.Schemas.Primitives;

public record ObjectSchema() : PrimitiveSchema(JsonValueType.Object)
{
    public int? MinProperties { get; init; } = null;

    public int? MaxProperties { get; init; } = null;

    public OrderedDictionary<string, SnapshotSchema>? OptionalProperties { get; init; } = null;

    public OrderedDictionary<string, SnapshotSchema>? RequiredProperties { get; init; } = null;

    /// <summary>
    /// Schema for additional properties. If null, additional properties are not allowed.
    /// </summary>
    public SnapshotSchema? AdditionalProperties { get; init; } = null;

    public List<(Regex Pattern, SnapshotSchema Model)>? PatternProperties { get; init; } = null;

    protected override void OnGenerate(ObjectValue schema)
    {
        if (OptionalProperties?.Count > 0 || RequiredProperties?.Count > 0)
        {
            var properties = schema.CreateNode("properties").AssignObject();

            if (RequiredProperties?.Count > 0)
                foreach (var (name, property) in RequiredProperties)
                    property.Generate(properties.CreateNode(name).AssignObject());

            if (OptionalProperties?.Count > 0)
                foreach (var (name, property) in OptionalProperties)
                    property.Generate(properties.CreateNode(name).AssignObject());
        }

        if (RequiredProperties?.Count > 0)
        {
            var propertyNames = schema.CreateNode("required").AssignArray();

            foreach (var (name, _) in RequiredProperties)
                propertyNames.CreateNode().AssignValue(name);
        }

        var additionalProperties = schema.CreateNode("additionalProperties");
        if (AdditionalProperties == null)
            additionalProperties.AssignValue(false);
        else
            AdditionalProperties.Generate(additionalProperties.AssignObject());

        if (MinProperties != null)
            schema.CreateNode("minProperties").AssignValue(MinProperties.Value);

        if (MaxProperties != null)
            schema.CreateNode("maxProperties").AssignValue(MaxProperties.Value);

        if (PatternProperties?.Count > 0)
        {
            var patternProperties = schema.CreateNode("patternProperties").AssignObject();
            foreach (var (pattern, model) in PatternProperties)
                model.Generate(patternProperties.CreateNode(pattern.ToString()).AssignObject());
        }
    }

    protected override bool OnValidate(SnapshotNode node)
    {
        if (node.Value is not ObjectValue document)
            return false;

        if (MinProperties != null && document.Count < MinProperties)
            return false;
        if (MaxProperties != null && document.Count > MaxProperties)
            return false;

        var propertySchemas = new (SnapshotNode, SnapshotSchema)[document.Count];
        var counterRequiredProperty = 0;
        foreach (var (index, propertyNode) in document.Nodes.Index())
        {
            if (RequiredProperties?.TryGetValue(propertyNode.Key, out var propertySchema) == true)
            {
                ++counterRequiredProperty;
                propertySchemas[index] = (propertyNode.Value, propertySchema);
                continue;
            }

            if (OptionalProperties?.TryGetValue(propertyNode.Key, out propertySchema) == true)
            {
                propertySchemas[index] = (propertyNode.Value, propertySchema);
                continue;
            }

            if (AdditionalProperties == null)
                return false;
            propertySchemas[index] = (propertyNode.Value, AdditionalProperties);
        }

        if (counterRequiredProperty != RequiredProperties?.Count)
            return false;

        foreach (var (propertyNode, propretySchema) in propertySchemas)
        {
            if (!propretySchema.Validate(propertyNode))
                return false;
        }

        if (PatternProperties == null || PatternProperties.Count == 0)
            return true;

        // Check for pattern properties.
        foreach (var (name, propertyNode) in document.Nodes)
        {
            foreach (var (pattern, schema) in PatternProperties)
            {
                if (pattern.IsMatch(name) && !schema.Validate(propertyNode))
                    return false;
            }
        }

        return true;
    }
}