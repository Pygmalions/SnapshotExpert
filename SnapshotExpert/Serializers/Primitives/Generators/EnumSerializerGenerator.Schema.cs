using System.Text;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Composite;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values.Primitives;

namespace SnapshotExpert.Serializers.Primitives.Generators;

internal static partial class EnumSerializerGenerator
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
            Title = $"Enum '{typeof(TEnum).Name}'",
            Schemas =
            [
                new StringSchema
                {
                    Title = "Encoded By Name",
                    EnumValues = values
                        .Select(value => new StringValue(value.ToString()!))
                        .ToArray()           
                },
                new IntegerSchema
                {
                    Title = "Encoded By Value",
                    Description = description.ToString(),
                    EnumValues = values
                        .Select(value => new Integer64Value(Convert.ToInt64(value)))
                        .ToArray()
                }
            ]
        };
    }
}