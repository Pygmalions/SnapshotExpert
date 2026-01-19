using System.Text.RegularExpressions;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Data.Schemas.Primitives;

public record ObjectSchema() : PrimitiveSchema(JsonValueType.Object)
{
    public int? MinProperties { get; init; }

    public int? MaxProperties { get; init; }

    /// <summary>
    /// Optional properties.
    /// </summary>
    public IReadOnlyDictionary<string, SnapshotSchema>? OptionalProperties { get; init; }

    /// <summary>
    /// Required properties.
    /// </summary>
    public IReadOnlyDictionary<string, SnapshotSchema>? RequiredProperties { get; init; }

    /// <summary>
    /// Schema for additional properties. If null, additional properties are not allowed.
    /// </summary>
    public SnapshotSchema? AdditionalProperties { get; init; }

    public List<(Regex Pattern, SnapshotSchema Model)>? PatternProperties { get; init; }

    protected override void OnGenerate(ObjectValue schema)
    {
        if (OptionalProperties != null || RequiredProperties != null)
        {
            var properties = schema.CreateNode("properties").AssignValue(new ObjectValue());

            if (RequiredProperties?.Count > 0)
                foreach (var (name, property) in RequiredProperties)
                    property.Generate(properties.CreateNode(name).AssignValue(new ObjectValue()));

            if (OptionalProperties?.Count > 0)
                foreach (var (name, property) in OptionalProperties)
                    property.Generate(properties.CreateNode(name).AssignValue(new ObjectValue()));
        }

        if (RequiredProperties != null)
        {
            var propertyNames = schema.CreateNode("required").AssignValue(new ArrayValue());

            foreach (var (name, _) in RequiredProperties)
                propertyNames.CreateNode().Value = name;
        }

        var additionalProperties = schema.CreateNode("additionalProperties");
        if (AdditionalProperties == null)
            additionalProperties.BindValue(false);
        else
            AdditionalProperties.Generate(additionalProperties.AssignValue(new ObjectValue()));

        if (MinProperties != null)
            schema.CreateNode("minProperties").BindValue(MinProperties.Value);

        if (MaxProperties != null)
            schema.CreateNode("maxProperties").BindValue(MaxProperties.Value);

        if (PatternProperties?.Count > 0)
        {
            var patternProperties = schema
                .CreateNode("patternProperties")
                .AssignValue(new ObjectValue());
            foreach (var (pattern, model) in PatternProperties)
                model.Generate(patternProperties
                    .CreateNode(pattern.ToString())
                    .AssignValue(new ObjectValue()));
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
        foreach (var (index, propertyNode) in document.DeclaredNodes.Index())
        {
            if (RequiredProperties?.TryGetValue(propertyNode.Name, out var propertySchema) == true)
            {
                ++counterRequiredProperty;
                propertySchemas[index] = (propertyNode, propertySchema);
                continue;
            }

            if (OptionalProperties?.TryGetValue(propertyNode.Name, out propertySchema) == true)
            {
                propertySchemas[index] = (propertyNode, propertySchema);
                continue;
            }

            if (AdditionalProperties == null)
                return false;
            propertySchemas[index] = (propertyNode, AdditionalProperties);
        }

        if (counterRequiredProperty != RequiredProperties?.Count)
            return false;

        foreach (var (propertyNode, propertySchema) in propertySchemas)
        {
            if (!propertySchema.Validate(propertyNode))
                return false;
        }

        if (PatternProperties == null || PatternProperties.Count == 0)
            return true;

        // Check for pattern properties.
        foreach (var propertyNode in document.DeclaredNodes)
        {
            foreach (var (pattern, schema) in PatternProperties)
            {
                if (pattern.IsMatch(propertyNode.Name) && !schema.Validate(propertyNode))
                    return false;
            }
        }

        return true;
    }
}