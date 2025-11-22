using System.Reflection;
using DocumentationParser;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;

namespace SnapshotExpert.Remoting;

public static class CallProxySchema
{
    /// <summary>
    /// Create a schema for arguments of a method call for the specified method.
    /// </summary>
    /// <param name="method">Method to create method call schema for.</param>
    /// <param name="serializers">Serializers to use.</param>
    /// <param name="documentation">Documentation to inject into the schema.</param>
    /// <returns>Snapshot schema for method calls of the specified method.</returns>
    public static ObjectSchema For(MethodInfo method, ISerializerProvider serializers,
        IDocumentationProvider? documentation = null)
    {
        OrderedDictionary<string, SnapshotSchema>? requiredProperties = null;
        OrderedDictionary<string, SnapshotSchema>? optionalProperties = null;

        foreach (var parameter in method.GetParameters())
        {
            var serializer = serializers.RequireSerializer(parameter.ParameterType);
            ref var selectedProperties =
                ref parameter.HasDefaultValue ? ref optionalProperties : ref requiredProperties;
            selectedProperties ??= new OrderedDictionary<string, SnapshotSchema>();
            selectedProperties.Add(parameter.Name ?? parameter.Position.ToString(), serializer.Schema);
        }

        return new ObjectSchema
        {
            Title = method.Name,
            Description = documentation?.GetEntry(method)?.Summary,
            RequiredProperties = requiredProperties,
            OptionalProperties = optionalProperties
        };
    }
}