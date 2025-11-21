using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;

namespace SnapshotExpert.Serializers.Primitives.Generators;

/// <summary>
/// Snapshot serializer for matrices (multidimensional arrays).
/// This serializer uses reflection to read and write elements of a matrix.
/// </summary>
/// <remarks>
/// Matrics such as <c>T[,]</c> and <c>T[,,]</c> are different types from
/// jagged arrays such as <c>T[][]</c> and <c>T[][][]</c>.
/// This serializer cannot handle jagged arrays;
/// they can be handled by <see cref="ArraySnapshotSerializer{TElement}"/>.
/// </remarks>
/// <typeparam name="TMatrix">Type of the matrix.</typeparam>
/// <typeparam name="TElement">Type of the element for the matrix.</typeparam>
public abstract class MatrixSnapshotSerializerBase<TMatrix, TElement>
    : SnapshotSerializerClassTypeBase<TMatrix> where TMatrix : class
{
    public required SnapshotSerializer<TElement> ElementSerializer { get; init; }

    public override void NewInstance(out TMatrix instance) => instance = null!;

    protected override SnapshotSchema GenerateSchema()
    {
        return MatrixSerializerGenerator.GenerateSchema(
            typeof(TMatrix).GetArrayRank(),
            ElementSerializer.Schema);
    }

    /// <summary>
    /// Verify the schema of the matrix and scan its shape.
    /// </summary>
    /// <param name="rootNode">Root value of ths matrix snapshot.</param>
    /// <param name="shape">Scanned shape will be written into this span.</param>
    /// <exception cref="Exception">
    /// Throw when the schema of the snapshot is invalid.
    /// </exception>
    protected static void ScanMatrixShape(SnapshotNode rootNode, Span<int> shape)
    {
        if (shape.Length == 0)
            return;
        if (rootNode.Value is not ArrayValue rootArray)
            throw new Exception("Failed to load snapshot for matrix: snapshot value is not an array.");
        shape[0] = rootArray.Count;
        var layer = rootArray;
        for (var dimension = 1; dimension < shape.Length; ++dimension)
        {
            int? length = null;
            foreach (var subnode in layer.DeclaredNodes)
            {
                if (subnode.Value is not ArrayValue subarray)
                    throw new Exception(
                        $"Failed to load snapshot for matrix: content in dimension {dimension} is not an array.");
                if (length == null)
                {
                    length = subarray.Count;
                    continue;
                }

                if (length != subarray.Count)
                    throw new Exception(
                        $"Failed to load snapshot for matrix: inconsistent array lengths in dimension {dimension}.");
            }

            shape[dimension] = length ?? throw new Exception(
                $"Failed to load snapshot for matrix: array in dimension {dimension} is empty.");
            layer = (ArrayValue)layer[0].Value!;
        }
    }
}