using SnapshotExpert.Data.Schemas.Primitives;

namespace SnapshotExpert.Data.Schemas.Utilities;

public class ObjectSchemaBuilder
{
    /// <summary>
    /// Title of the schema.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Description of the schema.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// If true, then this schema can accept null value.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Required properties of this object.
    /// </summary>
    public Dictionary<string, SnapshotSchema> RequiredProperties
        => field ??= new Dictionary<string, SnapshotSchema>();

    /// <summary>
    /// Optional properties of this object.
    /// </summary>
    public Dictionary<string, SnapshotSchema> OptionalProperties
        => field ??= new Dictionary<string, SnapshotSchema>();

    /// <summary>
    /// Build an object schema from this builder.
    /// </summary>
    /// <returns>Object schema.</returns>
    public ObjectSchema Build()
    {
        return new ObjectSchema
        {
            Title = Title,
            Description = Description,
            IsNullable = IsNullable,
            RequiredProperties = RequiredProperties,
            OptionalProperties = OptionalProperties
        };
    }
}