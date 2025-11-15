using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;

namespace SnapshotExpert.Serializers.Primitives.Generators;

internal static partial class MatrixSerializerGenerator
{
    public static SnapshotSchema GenerateSchema(int rank, SnapshotSchema elementSchema)
    {
        var current = new ArraySchema()
        {
            Title = $"Matrix Dimension {rank - 1}",
            Items = elementSchema
        };
        for (var dimension = rank - 2; dimension >= 0; --dimension)
        {
            current = new ArraySchema
            {
                Title = $"Matrix Dimension {dimension}",
                Items = current
            };
        }

        return current;
    }
}