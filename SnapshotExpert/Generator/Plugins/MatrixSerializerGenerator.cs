using System.Reflection;
using System.Reflection.Emit;
using EmitToolbox.Extensions;
using EmitToolbox.Framework;
using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Values;

namespace SnapshotExpert.Generator.Plugins;

internal static class MatrixSerializerGenerator
{
    public static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type matrixType)
    {
        var elementType = matrixType.GetElementType()!;
        var baseType = typeof(MatrixSnapshotSerializerBase<,>).MakeGenericType(matrixType, elementType);
        var typeContext = assemblyContext.DefineClass(
            $"SnapshotSerializer_Matrix_{matrixType}", parent: baseType);

        var matrixRank = matrixType.GetArrayRank();

        var saverContext = typeContext.ActionBuilder.Override(
            baseType.GetMethod("OnSaveSnapshot",
                BindingFlags.NonPublic | BindingFlags.Instance)!);
        GenerateSaveSnapshotMethod(saverContext, baseType, matrixType, elementType, matrixRank);

        var loaderContext = typeContext.ActionBuilder.Override(
            baseType.GetMethod("OnLoadSnapshot",
                BindingFlags.NonPublic | BindingFlags.Instance)!);
        GenerateLoadSnapshotMethod(loaderContext, baseType, matrixType, elementType, matrixRank);
        
        typeContext.Build();

        return typeContext.BuildingType;
    }

    private static void GenerateSaveSnapshotMethod(
        DynamicAction methodContext, Type baseType, Type matrixType, Type elementType, int matrixRank)
    {
        var code = methodContext.Code;

        var variableElement = code.DeclareLocal(elementType);

        var variablesIndex = new LocalBuilder[matrixRank];
        var variablesLength = new LocalBuilder[matrixRank];

        // Initialize variables.
        for (var dimension = 0; dimension < matrixRank; ++dimension)
        {
            variablesIndex[dimension] = code.DeclareLocal(typeof(int));

            // Store the length of current dimension.
            var variableLength = code.DeclareLocal(typeof(int));
            variablesLength[dimension] = variableLength;
            code.LoadArgument_1();
            code.Emit(OpCodes.Ldind_Ref);
            code.LoadLiteral(dimension);
            code.CallVirtual(typeof(Array).GetMethod(nameof(Array.GetLength))!);
            code.StoreLocal(variableLength);
        }

        var variableRootNode = code.DeclareLocal(typeof(SnapshotNode));
        code.LoadArgument_2();
        code.StoreLocal(variableRootNode);

        RecursivelyGenerateForDimension(0, variableRootNode);

        code.MethodReturn();

        return;

        void RecursivelyGenerateForDimension(int currentDimension, LocalBuilder variableCurrentNode)
        {
            var variableLength = variablesLength[currentDimension];
            var variableIndex = variablesIndex[currentDimension];

            // Initialize the index to zero.
            code.LoadLiteral(0);
            code.StoreLocal(variableIndex);

            // Create the array for the current dimension.
            var variableArray = code.DeclareLocal(typeof(ArrayValue));
            code.LoadLocal(variableLength);
            code.NewObject(typeof(ArrayValue).GetConstructor([typeof(int)])!);
            code.StoreLocal(variableArray);

            // Mark the beginning of the loop.
            var labelLoop = code.DefineLabel();
            code.MarkLabel(labelLoop);

            // Create node for sub-element.
            var variableSubNode = code.DeclareLocal(typeof(SnapshotNode));
            code.LoadLocal(variableArray);
            code.Call(typeof(ArrayValue).GetMethod(nameof(ArrayValue.CreateNode), Type.EmptyTypes)!);
            code.StoreLocal(variableSubNode);

            if (currentDimension < matrixRank - 1)
            {
                // Generate the next dimension.
                RecursivelyGenerateForDimension(currentDimension + 1, variableSubNode);
            }
            else
            {
                // Load the element.
                code.LoadArgument_1();
                code.Emit(OpCodes.Ldind_Ref);
                foreach (var variableIndexArgument in variablesIndex)
                    code.LoadLocal(variableIndexArgument);
                code.Call(matrixType.GetMethod("Get")!);
                code.StoreLocal(variableElement);

                // Use element serializer to serialize the element.
                code.LoadArgument_0();
                code.LoadProperty(baseType.GetProperty(nameof(MatrixSnapshotSerializerBase<,>.ElementSerializer))!);
                code.LoadLocalAddress(variableElement);
                code.LoadLocal(variableSubNode);
                code.LoadArgument_3();
                code.CallVirtual(typeof(SnapshotSerializer<>)
                    .MakeGenericType(elementType)
                    .GetMethod("SaveSnapshot",
                        [elementType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotWritingScope)])!);
            }

            // index += 1
            code.LoadLocal(variableIndex);
            code.LoadLiteral(1);
            code.Add();
            code.StoreLocal(variableIndex);

            // Continue the loop if: index < length.
            code.LoadLocal(variableIndex);
            code.LoadLocal(variableLength);
            code.GotoIfLess(labelLoop);

            // Store the array into the current node.
            code.LoadLocal(variableCurrentNode);
            code.LoadLocal(variableArray);
            code.StoreProperty(typeof(SnapshotNode).GetProperty(nameof(SnapshotNode.Value))!);
        }
    }

    private static void GenerateLoadSnapshotMethod(
        DynamicAction methodContext, Type baseType, Type matrixType, Type elementType, int matrixRank)
    {
        var code = methodContext.Code;

        var variableElement = code.DeclareLocal(elementType);

        var variableShape = code.DeclareLocal(typeof(Span<int>));
        code.AllocateSpanOnStack<int>(matrixRank);
        code.StoreLocal(variableShape);

        // Load the shape of the matrix from the snapshot node.
        code.LoadArgument_2();
        code.LoadLocal(variableShape);
        code.Call(baseType.GetMethod("ScanMatrixShape",
            BindingFlags.Static | BindingFlags.NonPublic)!);

        var variablesIndex = new LocalBuilder[matrixRank];
        var variablesLength = new LocalBuilder[matrixRank];

        // Initialize variables.
        for (var dimension = 0; dimension < matrixRank; ++dimension)
        {
            variablesIndex[dimension] = code.DeclareLocal(typeof(int));

            // Store the length of current dimension.
            var variableLength = code.DeclareLocal(typeof(int));
            variablesLength[dimension] = variableLength;

            code.LoadLocalAddress(variableShape);
            code.GetSpanItemReference<int>(dimension);
            code.Emit(OpCodes.Ldind_I4);
            code.StoreLocal(variableLength);
        }

        // Check if the matrix has the same shape.
        var labelBeginInitialization = code.DefineLabel();
        var labelEndInitialization = code.DefineLabel();

        // If the mode is not patching, initialize the matrix.
        code.LoadArgument_2();
        code.LoadProperty(typeof(SnapshotNode).GetProperty(nameof(SnapshotNode.Mode))!);
        code.LoadLiteral((int)SnapshotModeType.Patching);
        code.GotoIfNotEqual(labelBeginInitialization);

        // If the matrix is null, initialize the matrix.
        code.LoadArgument_1();
        code.Emit(OpCodes.Ldind_Ref);
        code.GotoIfFalse(labelBeginInitialization);

        // If the matrix has different shape, initialize the matrix.
        foreach (var (dimension, variableLength) in variablesLength.Index())
        {
            code.LoadArgument_1();
            code.Emit(OpCodes.Ldind_Ref);
            code.LoadLiteral(dimension);
            code.CallVirtual(typeof(Array).GetMethod(nameof(Array.GetLength))!);
            code.LoadLocal(variableLength);
            code.GotoIfNotEqual(labelBeginInitialization);
        }

        // Branch: The matrix has the same shape, skip initialization.
        code.Goto(labelEndInitialization);

        // Branch: The matrix has a different shape, initialize it.
        code.MarkLabel(labelBeginInitialization);

        // Initialize the matrix with a new instance.
        code.LoadArgument_1();
        foreach (var variableLength in variablesLength)
            code.LoadLocal(variableLength);
        code.NewObject(matrixType.GetConstructor(
            Enumerable.Repeat(elementType, matrixRank).ToArray())!);
        code.Emit(OpCodes.Stind_Ref);
        code.MarkLabel(labelEndInitialization);

        var variableRootNode = code.DeclareLocal(typeof(SnapshotNode));
        code.LoadArgument_2();
        code.StoreLocal(variableRootNode);

        RecursivelyGenerateForDimension(0, variableRootNode);

        code.MethodReturn();

        return;

        void RecursivelyGenerateForDimension(int currentDimension, LocalBuilder variableCurrentNode)
        {
            var variableLength = variablesLength[currentDimension];
            var variableIndex = variablesIndex[currentDimension];

            // Initialize the index to zero.
            code.LoadLiteral(0);
            code.StoreLocal(variableIndex);

            // Create the array for the current dimension.
            var variableArray = code.DeclareLocal(typeof(ArrayValue));
            code.LoadLocal(variableCurrentNode);
            code.LoadProperty(typeof(SnapshotNode).GetProperty(nameof(SnapshotNode.Value))!);
            code.StoreLocal(variableArray);

            // Mark the beginning of the loop.
            var labelLoop = code.DefineLabel();
            code.MarkLabel(labelLoop);

            var variableSubNode = code.DeclareLocal(typeof(SnapshotNode));
            code.LoadLocal(variableArray);
            code.LoadLocal(variableIndex);
            code.CallVirtual(typeof(ArrayValue).GetMethod("get_Item", [typeof(int)])!);
            code.StoreLocal(variableSubNode);

            if (currentDimension < matrixRank - 1)
            {
                // Generate the next dimension.
                RecursivelyGenerateForDimension(currentDimension + 1, variableSubNode);
            }
            else
            {
                // Load the element.
                code.LoadArgument_1();
                code.Emit(OpCodes.Ldind_Ref);
                foreach (var variableIndexArgument in variablesIndex)
                    code.LoadLocal(variableIndexArgument);
                code.Call(matrixType.GetMethod("Get")!);
                code.StoreLocal(variableElement);
                
                // Skip initialization if the element is not null.
                var labelSkipElementInitialization = code.DefineLabel();
                code.LoadLocal(variableElement);
                code.GotoIfTrue(labelSkipElementInitialization);

                // Initialize the element if it is null.
                code.LoadArgument_0();
                code.LoadProperty(baseType.GetProperty(
                    nameof(MatrixSnapshotSerializerBase<,>.ElementSerializer))!);
                code.LoadLocalAddress(variableElement);
                code.CallVirtual(
                    typeof(SnapshotSerializer<>).MakeGenericType(elementType)
                        .GetMethod(nameof(SnapshotSerializer<>.NewInstance),
                            [elementType.MakeByRefType()])!);
                code.MarkLabel(labelSkipElementInitialization);

                // Use element serializer to deserialize the element.
                code.LoadArgument_0();
                code.LoadProperty(baseType.GetProperty(
                    nameof(MatrixSnapshotSerializerBase<,>.ElementSerializer))!);
                code.LoadLocalAddress(variableElement);
                code.LoadLocal(variableSubNode);
                code.LoadArgument_3();
                code.CallVirtual(typeof(SnapshotSerializer<>).MakeGenericType(elementType)
                    .GetMethod("LoadSnapshot",
                        [elementType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotReadingScope)])!);

                // Store the element into the matrix.
                code.LoadArgument_1();
                code.Emit(OpCodes.Ldind_Ref);
                foreach (var variableIndexArgument in variablesIndex)
                    code.LoadLocal(variableIndexArgument);
                code.LoadLocal(variableElement);
                code.Call(matrixType.GetMethod("Set")!);
            }

            // index += 1
            code.LoadLocal(variableIndex);
            code.LoadLiteral(1);
            code.Add();
            code.StoreLocal(variableIndex);

            // Continue the loop if: index < length.
            code.LoadLocal(variableIndex);
            code.LoadLocal(variableLength);
            code.GotoIfLess(labelLoop);
        }
    }
}