using System.Text;
using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Composite;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values.Primitives;

namespace SnapshotExpert.Generator.Plugins;

public static class EnumSchemaGenerator
{
    public static SnapshotSchema GenerateSchema<TEnum>() where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        
        var description = new StringBuilder();
        description.AppendLine("Names and corresponding values for the enum:");
        foreach (var value in values)
        {
            description.Append(" - ");
            description.Append(value);
            description.Append(": ");
            description.AppendLine(Convert.ToInt64(value).ToString());
        }

        return new OneOfSchema
        {
            Title = $"Enum '{typeof(TEnum)}'",
            Schemas =
            [
                new StringSchema
                {
                    Title = "Encoded By Name",
                    EnumValues = values
                        .OfType<object>()
                        .Select(value => new StringValue(value.ToString()!))
                        .ToArray()           
                },
                new IntegerSchema
                {
                    Title = "Encoded By Value",
                    Description = description.ToString(),
                    EnumValues = values
                        .OfType<object>()
                        .Select(value => new Integer64Value(Convert.ToInt64(value)))
                        .ToArray()
                }
            ]
        };
    }
}