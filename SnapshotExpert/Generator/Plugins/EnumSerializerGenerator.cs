using System.Reflection;
using System.Reflection.Emit;
using EmitToolbox.Extensions;
using EmitToolbox.Framework;
using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Values.Primitives;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Generator.Plugins;

internal static class EnumSerializerGenerator
{
    private enum EnumUnderlyingType
    {
        SByte = 0,
        Byte = 1,
        Int16 = 10,
        UInt16 = 11,
        Int32 = 20,
        UInt32 = 21,
        Int64 = 30,
        UInt64 = 31
    }

    public static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type enumType)
    {
        var typeContext = assemblyContext.DefineClass($"SnapshotSerializer_Enum_{enumType}",
            parent: typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(enumType));
        var baseType = typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(enumType);

        var underlyingType = enumType.GetEnumUnderlyingType() switch
        {
            { } type when type == typeof(sbyte) => EnumUnderlyingType.SByte,
            { } type when type == typeof(byte) => EnumUnderlyingType.Byte,
            { } type when type == typeof(short) => EnumUnderlyingType.Int16,
            { } type when type == typeof(ushort) => EnumUnderlyingType.UInt16,
            { } type when type == typeof(int) => EnumUnderlyingType.Int32,
            { } type when type == typeof(uint) => EnumUnderlyingType.UInt32,
            { } type when type == typeof(long) => EnumUnderlyingType.Int64,
            { } type when type == typeof(ulong) => EnumUnderlyingType.UInt64,
            _ => throw new Exception($"Unsupported enum underlying type: '{enumType.GetEnumUnderlyingType()}'.")
        };

        var enumEntries = Enum.GetValues(enumType);

        var saverContext = typeContext.ActionBuilder.Override(
            baseType.GetMethod(nameof(SnapshotSerializerValueTypeBase<>.SaveSnapshot),
                [enumType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotWritingScope)])!);
        GenerateSaveSnapshotMethod(saverContext, underlyingType, enumType, enumEntries);
        
        var loaderContext = typeContext.ActionBuilder.Override(
            baseType.GetMethod(nameof(SnapshotSerializerValueTypeBase<>.LoadSnapshot),
                [enumType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotReadingScope)])!);
        GenerateLoadSnapshotMethod(loaderContext, underlyingType, enumType, enumEntries);

        var schemaGeneratorContext = typeContext.FunctorBuilder.Override(
            typeof(SnapshotSerializer).GetMethod("GenerateSchema", 
                BindingFlags.NonPublic | BindingFlags.Instance)!);
        GenerateSchemaGenerator(schemaGeneratorContext, enumType);
        
        typeContext.Build();

        return typeContext.BuildingType;
    }

    private static void GenerateSchemaGenerator(DynamicFunctor methodContext, Type enumType)
    {
        var code = methodContext.Code;
        
        code.Call(typeof(EnumSchemaGenerator)
            .GetMethod(nameof(EnumSchemaGenerator.GenerateSchema))!
            .MakeGenericMethod(enumType));
        
        code.MethodReturn();
    }
    
    private static void GenerateSaveSnapshotMethod(
        DynamicAction methodContext, EnumUnderlyingType underlyingType,
        Type enumType, Array enumEntries)
    {
        var code = methodContext.Code;

        // Serialized snapshot value.
        var variableSnapshotValue = code.DeclareLocal(typeof(SnapshotValue));
        // Number representation of the enum value.
        var variableEnumInteger = code.DeclareLocal(
            underlyingType < EnumUnderlyingType.Int64 ? typeof(int) : typeof(long));

        // Load target and convert to an integer representation.
        code.LoadArgument(1);
        code.Emit(underlyingType switch
        {
            EnumUnderlyingType.SByte or EnumUnderlyingType.Byte => OpCodes.Ldind_I1,
            EnumUnderlyingType.Int16 or EnumUnderlyingType.UInt16 => OpCodes.Ldind_I2,
            EnumUnderlyingType.Int32 or EnumUnderlyingType.UInt32 => OpCodes.Ldind_I4,
            EnumUnderlyingType.Int64 or EnumUnderlyingType.UInt64 => OpCodes.Ldind_I8,
            _ => throw new ArgumentOutOfRangeException(nameof(underlyingType), underlyingType, null)
        });
        code.Emit(OpCodes.Stloc, variableEnumInteger);

        var labelTextual = code.DefineLabel();
        var labelEnding = code.DefineLabel();

        // Check the encoding format of the scope.
        code.LoadArgument(3);
        code.Emit(OpCodes.Call,
            typeof(SnapshotWritingScope)
                .GetProperty(nameof(SnapshotWritingScope.Format))!.GetGetMethod()!);
        code.Emit(OpCodes.Ldc_I4, (int)SnapshotDataFormat.Textual);
        code.GotoIfEqual(labelTextual);

        // Serializer as an integer value.
        // Note: no range check is performed for integer representation.
        {
            code.LoadLocal(variableEnumInteger);
            code.NewObject(underlyingType < EnumUnderlyingType.Int64
                ? typeof(Integer32Value).GetConstructor([typeof(int)])!
                : typeof(Integer64Value).GetConstructor([typeof(long)])!);
            code.Emit(OpCodes.Stloc, variableSnapshotValue);
            code.Goto(labelEnding);
        }
        
        // Serialize as a string value.
        {
            code.MarkLabel(labelTextual);

            // Load target and convert to a string value.
            var entryLabels = new Label[enumEntries.Length];
            for (var index = 0; index < entryLabels.Length; index++)
                entryLabels[index] = code.DefineLabel();

            // Matching enum entries and jump to the corresponding label.
            for (var index = 0; index < entryLabels.Length; ++index)
            {
                code.LoadLocal(variableEnumInteger);
                if (underlyingType < EnumUnderlyingType.Int64)
                    code.LoadLiteral(Convert.ToInt32(enumEntries.GetValue(index)));
                else
                    code.LoadLiteral(Convert.ToInt64(enumEntries.GetValue(index)));
                code.GotoIfEqual(entryLabels[index]);
            }

            // Throw if no matching entry is found.
            code.LoadLiteral($"Failed to save snapshot for enum '{enumType}': enum value out of range.");
            code.NewObject(typeof(Exception).GetConstructor([typeof(string)])!);
            code.Emit(OpCodes.Throw);
            
            for (var index = 0; index < entryLabels.Length; ++index)
            {
                code.MarkLabel(entryLabels[index]);
                code.LoadLiteral(enumEntries.GetValue(index)!.ToString()!);
                code.NewObject(typeof(StringValue).GetConstructor([typeof(string)])!);
                code.Emit(OpCodes.Stloc, variableSnapshotValue);

                code.Goto(labelEnding);
            }
        }

        code.MarkLabel(labelEnding);

        // Store the snapshot value into the node.
        code.LoadArgument(2);
        code.LoadLocal(variableSnapshotValue);
        code.StoreProperty(typeof(SnapshotNode).GetProperty(nameof(SnapshotNode.Value))!);
        code.MethodReturn();
    }

    private static void GenerateLoadSnapshotMethod(
        DynamicAction methodContext, EnumUnderlyingType underlyingType,
        Type enumType, Array enumEntries)
    {
        var code = methodContext.Code;

        var variableEnumInteger = code.DeclareLocal(
            underlyingType < EnumUnderlyingType.Int64 ? typeof(int) : typeof(long));
        var variableSnapshotValue = code.DeclareLocal(typeof(SnapshotValue));

        // Store the snapshot value.
        code.LoadArgument(2);
        code.LoadProperty(typeof(SnapshotNode).GetProperty(nameof(SnapshotNode.Value))!);
        code.StoreLocal(variableSnapshotValue);

        var labelRetrievingFromString = code.DefineLabel();
        var labelRetrievingFromInteger32 = code.DefineLabel();
        var labelRetrievingFromInteger64 = code.DefineLabel();
        var labelConverting = code.DefineLabel();

        code.LoadLocal(variableSnapshotValue);
        code.Emit(OpCodes.Isinst, typeof(StringValue));
        code.GotoIfTrue(labelRetrievingFromString);
        code.LoadLocal(variableSnapshotValue);
        code.Emit(OpCodes.Isinst, typeof(Integer32Value));
        code.GotoIfTrue(labelRetrievingFromInteger32);
        code.LoadLocal(variableSnapshotValue);
        code.Emit(OpCodes.Isinst, typeof(Integer64Value));
        code.GotoIfTrue(labelRetrievingFromInteger64);

        code.LoadLiteral($"Failed to load snapshot for enum '{enumType}': unexpected snapshot value.");
        code.NewObject(typeof(Exception).GetConstructor([typeof(string)])!);
        code.Emit(OpCodes.Throw);

        // Parse from a string value.
        {
            code.MarkLabel(labelRetrievingFromString);
            var variableEnumString = code.DeclareLocal(typeof(string));
            code.LoadLocal(variableSnapshotValue);
            code.LoadProperty(typeof(StringValue).GetProperty(nameof(StringValue.Value))!);
            code.StoreLocal(variableEnumString);
            
            foreach (var enumEntry in enumEntries)
            {
                var labelNext = code.DefineLabel();
                code.LoadLocal(variableEnumString);
                code.LoadLiteral(enumEntry!.ToString()!);
                code.Call(typeof(string).GetMethod("Equals",
                    BindingFlags.Static | BindingFlags.Public,
                    [typeof(string), typeof(string)])!);
                code.GotoIfFalse(labelNext);
            
                if (underlyingType < EnumUnderlyingType.Int64)
                    code.LoadLiteral(Convert.ToInt32(enumEntry));
                else
                    code.LoadLiteral(Convert.ToInt64(enumEntry));
                code.StoreLocal(variableEnumInteger);
                code.Goto(labelConverting);
            
                code.MarkLabel(labelNext);
            }
        }
        
        // Parse from a 32-bit integer value.
        // Note: no range check is performed for integer representation.
        {
            code.MarkLabel(labelRetrievingFromInteger32);
            code.LoadLocal(variableSnapshotValue);
            code.LoadProperty(typeof(Integer32Value).GetProperty(nameof(Integer32Value.Value))!);
            code.StoreLocal(variableEnumInteger);
            code.Goto(labelConverting);
        }
        
        // Parse from a 32-bit integer value.
        // Note: no range check is performed for integer representation.
        {
            code.MarkLabel(labelRetrievingFromInteger64);
            code.LoadLocal(variableSnapshotValue);
            code.LoadProperty(typeof(Integer64Value).GetProperty(nameof(Integer64Value.Value))!);
            code.StoreLocal(variableEnumInteger);
            code.Goto(labelConverting);
        }
        
        code.MarkLabel(labelConverting);
        code.LoadArgument(1);
        code.LoadLocal(variableEnumInteger);
        switch (underlyingType)
        {
            case EnumUnderlyingType.SByte:
                code.Emit(OpCodes.Conv_I1);
                code.Emit(OpCodes.Stind_I1);
                break;
            case EnumUnderlyingType.Byte:
                code.Emit(OpCodes.Conv_U1);
                code.Emit(OpCodes.Stind_I1);
                break;
            case EnumUnderlyingType.Int16:
                code.Emit(OpCodes.Conv_I2);
                code.Emit(OpCodes.Stind_I2);
                break;
            case EnumUnderlyingType.UInt16:
                code.Emit(OpCodes.Conv_U2);
                code.Emit(OpCodes.Stind_I2);
                break;
            case EnumUnderlyingType.Int32:
                code.Emit(OpCodes.Conv_I4);
                code.Emit(OpCodes.Stind_I4);
                break;
            case EnumUnderlyingType.UInt32:
                code.Emit(OpCodes.Conv_U4);
                code.Emit(OpCodes.Stind_I4);
                break;
            case EnumUnderlyingType.Int64:
                code.Emit(OpCodes.Conv_I8);
                code.Emit(OpCodes.Stind_I8);
                break;
            case EnumUnderlyingType.UInt64:
                code.Emit(OpCodes.Conv_U8);
                code.Emit(OpCodes.Stind_I8);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(underlyingType), underlyingType, null);
        }

        code.MethodReturn();
    }
}