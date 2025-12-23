using System.Reflection;
using System.Reflection.Emit;
using EmitToolbox;
using EmitToolbox.Builders;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Utilities;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Serializers.Primitives.Generators;

public static class DebugTest
{
    public static void Debug(Array array)
    {
        return;
    }
}

internal static partial class MatrixSerializerGenerator
{
    private static readonly DynamicResourceForType<Type> GeneratedSerializerTypes = new(GenerateSerializerType);

    public static Type GetSerializerType(Type enumType) => GeneratedSerializerTypes[enumType];

    public static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type matrixType)
    {
        var elementType = matrixType.GetElementType()!;
        var baseType = typeof(MatrixSnapshotSerializerBase<,>).MakeGenericType(matrixType, elementType);
        var typeContext = assemblyContext.DefineClass(
            matrixType.CreateDynamicFriendlyName("GeneratedMatrixSerializer_"),
            parent: baseType);

        var matrixRank = matrixType.GetArrayRank();

        GenerateSaveSnapshotMethod(typeContext, matrixType, elementType, matrixRank);
        GenerateLoadSnapshotMethod(typeContext, matrixType, elementType, matrixRank);

        typeContext.Build();

        return typeContext.BuildingType;
    }

    private static void GenerateSaveSnapshotMethod(
        DynamicType typeContext, Type matrixType, Type elementType, int matrixRank)
    {
        var baseType = typeof(MatrixSnapshotSerializerBase<,>).MakeGenericType(matrixType, elementType);
        var method = typeContext.MethodFactory.Instance.OverrideAction(
            baseType.GetMethod("OnSaveSnapshot",
                BindingFlags.NonPublic | BindingFlags.Instance)!);

        var argumentTarget = method.Argument(0, matrixType.MakeByRefType());
        var argumentNode = method.Argument<SnapshotNode>(1);
        var argumentScope = method.Argument<SnapshotWritingScope>(2);

        var variablesIndex = new VariableSymbol<int>[matrixRank];
        var variablesLength = new VariableSymbol<int>[matrixRank];

        // Use element serializer to serialize the element.
        var variableSerializer = method.This().Invoke(baseType
            .GetProperty(nameof(MatrixSnapshotSerializerBase<,>.ElementSerializer))!
            .GetMethod!)!;

        // Initialize variables.
        for (var dimension = 0; dimension < matrixRank; ++dimension)
        {
            variablesIndex[dimension] = method.Variable<int>();

            // Store the length of the current dimension.
            variablesLength[dimension] = argumentTarget
                .Invoke<int>(typeof(Array).GetMethod(nameof(Array.GetLength))!,
                    [method.Literal(dimension)])
                .ToSymbol();
        }

        RecursivelyGenerateForDimension(0, argumentNode);

        method.Return();

        return;

        void RecursivelyGenerateForDimension(int currentDimension, ISymbol<SnapshotNode> variableCurrentNode)
        {
            var variableLength = variablesLength[currentDimension];
            var variableIndex = variablesIndex[currentDimension];

            // Initialize the index to zero.
            variableIndex.AssignValue(0);

            // Create the array for the current dimension.
            var variableArray = method.New(
                () => new ArrayValue(Any<int>.Value), [variableLength]);

            // Mark the beginning of the loop.
            var labelLoop = method.DefineLabel();
            labelLoop.Mark();

            // Create node for sub-element.
            var variableSubNode = variableArray
                .Invoke(target => target.CreateNode())
                .ToSymbol();

            if (currentDimension < matrixRank - 1)
            {
                // Generate the next dimension.
                RecursivelyGenerateForDimension(currentDimension + 1, variableSubNode);
            }
            else
            {
                // Load the element.
                var variableElement = argumentTarget.Invoke(
                    matrixType.GetMethod("Get")!, variablesIndex)!;

                variableSerializer.Invoke(typeof(SnapshotSerializer<>)
                        .MakeGenericType(elementType)
                        .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                            [elementType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotWritingScope)])!,
                    [variableElement, variableSubNode, argumentScope]);
            }

            // index += 1
            variableIndex.AssignValue(variableIndex + 1);

            // Continue the loop if: index < length.
            labelLoop.GotoIfTrue(variableIndex < variableLength);

            // Store the array into the current node.
            variableCurrentNode.SetPropertyValue(target => target.Value, variableArray);
        }
    }

    private static void GenerateLoadSnapshotMethod(
        DynamicType typeContext, Type matrixType, Type elementType, int matrixRank)
    {
        var baseType = typeof(MatrixSnapshotSerializerBase<,>).MakeGenericType(matrixType, elementType);
        var method = typeContext.MethodFactory.Instance.OverrideAction(
            baseType.GetMethod("OnLoadSnapshot",
                BindingFlags.NonPublic | BindingFlags.Instance)!);

        var argumentTarget = method.Argument(0, matrixType.MakeByRefType());
        var argumentNode = method.Argument<SnapshotNode>(1);
        var argumentScope = method.Argument<SnapshotReadingScope>(2);

        var code = method.Code;

        var variableSerializer = method.This().Invoke(
            baseType.GetProperty(
                nameof(MatrixSnapshotSerializerBase<,>.ElementSerializer))!.GetMethod!)!;

        var variableShape = method.StackAllocate<int>(matrixRank);

        // Load the shape of the matrix from the snapshot node.
        method.Invoke(baseType.GetMethod("ScanMatrixShape",
                BindingFlags.Static | BindingFlags.NonPublic)!,
            [argumentNode, variableShape]);

        var variablesIndex = new VariableSymbol<int>[matrixRank];
        var variablesLength = new VariableSymbol<int>[matrixRank];

        // Initialize variables.
        for (var dimension = 0; dimension < matrixRank; ++dimension)
        {
            variablesIndex[dimension] = method.Variable<int>();

            // Store the length of the current dimension.
            var variableLength = method.Variable<int>();
            variablesLength[dimension] = variableLength;

            variableShape
                .ElementAt(dimension)
                .CopyValueTo(variableLength);
        }

        // Check if the matrix has the same shape.
        var labelBeginInitialization = method.DefineLabel();
        var labelEndInitialization = method.DefineLabel();

        // If the mode is not patching, initialize the matrix.
        labelBeginInitialization.GotoIfTrue(argumentNode
            .GetPropertyValue(target => target.Mode)
            .IsNotEqualTo(SnapshotModeType.Patching));

        // If the matrix is null, initialize the matrix.
        labelBeginInitialization.GotoIfFalse(argumentTarget.HasNotNullValue());

        // If the matrix has a different shape, initialize the matrix.
        foreach (var (dimension, variableLength) in variablesLength.Index())
        {
            labelBeginInitialization.GotoIfFalse(
                argumentTarget.Invoke<int>(typeof(Array).GetMethod(nameof(Array.GetLength))!,
                        [method.Literal(dimension)])
                    .IsEqualTo(variableLength));
        }

        // The matrix has the same shape, skip initialization.
        labelEndInitialization.Goto();

        // Branch: The matrix has a different shape, initialize it.
        labelBeginInitialization.Mark();

        // Initialize the matrix with a new instance.
        argumentTarget.AssignNew(
            matrixType.GetConstructor(
                Enumerable.Repeat(elementType, matrixRank).ToArray())!,
            variablesLength);

        labelEndInitialization.Mark();

        RecursivelyGenerateForDimension(0, argumentNode);

        method.Return();

        return;

        void RecursivelyGenerateForDimension(int currentDimension, ISymbol<SnapshotNode> variableCurrentNode)
        {
            var variableLength = variablesLength[currentDimension];
            var variableIndex = variablesIndex[currentDimension];

            // Initialize the index to zero.
            variableIndex.AssignValue(0);

            // Create the array for the current dimension.
            var variableArray = variableCurrentNode
                .GetPropertyValue(target => target.Value)
                .CastTo<ArrayValue>()
                .ToSymbol();

            // Mark the beginning of the loop.
            var labelLoop = method.DefineLabel();
            labelLoop.Mark();

            var variableSubNode = variableArray.Invoke<SnapshotNode>(
                typeof(ArrayValue).GetMethod("get_Item", [typeof(int)])!,
                [variableIndex]);

            if (currentDimension < matrixRank - 1)
            {
                // Generate the next dimension.
                RecursivelyGenerateForDimension(currentDimension + 1, variableSubNode);
            }
            else
            {
                // Load the element.
                var variableElement = argumentTarget.Invoke(
                    matrixType.GetMethod("Get")!, variablesIndex)!;

                // Initialize the element if it is of a reference type and it is null.
                if (!elementType.IsValueType)
                {
                    var labelSkipElementInitialization = method.DefineLabel();
                    variableElement.LoadContent();
                    code.Emit(OpCodes.Brtrue, labelSkipElementInitialization.Label);

                    // Initialize the element if it is null.
                    variableSerializer.Invoke(
                        typeof(SnapshotSerializer<>).MakeGenericType(elementType)
                            .GetMethod(nameof(SnapshotSerializer<>.NewInstance),
                                [elementType.MakeByRefType()])!,
                        [variableElement]);
                    labelSkipElementInitialization.Mark();
                }

                // Use element serializer to deserialize the element.
                variableSerializer.Invoke(
                    typeof(SnapshotSerializer<>).MakeGenericType(elementType)
                        .GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot),
                            [elementType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotReadingScope)])!,
                    [variableElement, variableSubNode, argumentScope]);

                // Store the element into the matrix.
                argumentTarget.Invoke(
                    matrixType.GetMethod("Set")!,
                    [..variablesIndex, variableElement]);
            }

            // index += 1
            variableIndex.AssignValue(variableIndex + 1);

            // Continue the loop if: index < length.
            labelLoop.GotoIfTrue(variableIndex < variableLength);
        }
    }
}