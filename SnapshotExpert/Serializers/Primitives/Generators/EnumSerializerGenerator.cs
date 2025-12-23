using System.Reflection;
using System.Reflection.Emit;
using EmitToolbox;
using EmitToolbox.Builders;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Utilities;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Serializers.Primitives.Generators;

internal static partial class EnumSerializerGenerator
{
    private static readonly DynamicResourceForType<Type> GeneratedSerializerTypes = new(GenerateSerializerType);

    public static Type GetSerializerType(Type enumType) => GeneratedSerializerTypes[enumType];

    public static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type enumType)
    {
        var baseType = typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(enumType);
        var typeContext = assemblyContext.DefineClass(
            enumType.CreateDynamicFriendlyName("GeneratedEnumSerializer_"),
            parent: baseType);

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

        GenerateSaveSnapshotMethod(typeContext, underlyingType, enumType, enumEntries);
        GenerateLoadSnapshotMethod(typeContext, underlyingType, enumType, enumEntries);
        GenerateSchemaGenerator(typeContext, enumType);

        typeContext.Build();

        return typeContext.BuildingType;
    }

    private static void GenerateSchemaGenerator(DynamicType typeContext, Type enumType)
    {
        var method =
            typeContext.MethodFactory.Instance.OverrideFunctor<SnapshotSchema>(
                typeof(SnapshotSerializer).GetMethod("GenerateSchema",
                    BindingFlags.NonPublic | BindingFlags.Instance)!);
        var schema = method.Invoke<SnapshotSchema>(
            typeof(EnumSerializerGenerator)
                .GetMethod(nameof(GenerateSchema))!
                .MakeGenericMethod(enumType));
        method.Return(schema);
    }

    private static void GenerateSaveSnapshotMethod(
        DynamicType typeContext, EnumUnderlyingType underlyingType, Type enumType, Array enumEntries)
    {
        var baseType = typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(enumType);

        var method = typeContext.MethodFactory.Instance.OverrideAction(
            baseType.GetMethod(nameof(SnapshotSerializerValueTypeBase<>.SaveSnapshot),
                [enumType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotWritingScope)])!);

        var code = method.Code;

        var argumentTarget = method.Argument(0, enumType.MakeByRefType());
        var argumentNode = method.Argument<SnapshotNode>(1);
        var argumentScope = method.Argument<SnapshotWritingScope>(2);

        // Serialized snapshot value.
        var variableSnapshotValue = method.Variable<SnapshotValue>();

        // Load the target and convert to an integer representation.
        var variableEnumInteger = method.Variable(
            underlyingType < EnumUnderlyingType.Int64 ? typeof(int) : typeof(long)
        );

        argumentTarget.LoadContent();
        code.Emit(underlyingType switch
        {
            EnumUnderlyingType.SByte or EnumUnderlyingType.Byte => OpCodes.Ldind_I1,
            EnumUnderlyingType.Int16 or EnumUnderlyingType.UInt16 => OpCodes.Ldind_I2,
            EnumUnderlyingType.Int32 or EnumUnderlyingType.UInt32 => OpCodes.Ldind_I4,
            EnumUnderlyingType.Int64 or EnumUnderlyingType.UInt64 => OpCodes.Ldind_I8,
            _ => throw new ArgumentOutOfRangeException(nameof(underlyingType), underlyingType, null)
        });
        variableEnumInteger.StoreContent();

        method.IfElse(argumentScope
                .GetPropertyValue(target => target.Format)
                .IsEqualTo(SnapshotDataFormat.Binary),
            onTrue: () =>
            {
                variableSnapshotValue.AssignNew(underlyingType < EnumUnderlyingType.Int64
                        ? typeof(Integer32Value).GetConstructor([typeof(int)])!
                        : typeof(Integer64Value).GetConstructor([typeof(long)])!,
                    [variableEnumInteger]);
            },
            onFalse: () =>
            {
                var labelEnd = method.DefineLabel();

                // Load the target and convert to a string value.
                var entryLabels = new Label[enumEntries.Length];
                for (var index = 0; index < entryLabels.Length; index++)
                    entryLabels[index] = code.DefineLabel();

                // Matching enum entries and jump to the corresponding label.
                for (var index = 0; index < entryLabels.Length; ++index)
                {
                    variableEnumInteger.LoadContent();
                    if (underlyingType < EnumUnderlyingType.Int64)
                        method.Literal(Convert.ToInt32(enumEntries.GetValue(index))).LoadContent();
                    else
                        method.Literal(Convert.ToInt64(enumEntries.GetValue(index))).LoadContent();
                    code.Emit(OpCodes.Beq, entryLabels[index]);
                }

                // Throw if no matching entry is found.
                method.New(() => new Exception(Any<string>.Value),
                    [
                        method.Literal(
                            $"Failed to save snapshot for enum '{enumType}': enum value out of range.")
                    ])
                    .LoadContent();
                code.Emit(OpCodes.Throw);

                for (var index = 0; index < entryLabels.Length; ++index)
                {
                    code.MarkLabel(entryLabels[index]);

                    variableSnapshotValue.AssignContent(
                        method.New(
                            () => new StringValue(Any<string>.Value),
                            [method.Literal(enumEntries.GetValue(index)!.ToString()!)])
                    );

                    code.Emit(OpCodes.Br, labelEnd.Label);
                }

                labelEnd.Mark();
            });

        argumentNode.SetPropertyValue(
            target => target.Value, variableSnapshotValue);
        method.Return();
    }

    private static void GenerateLoadSnapshotMethod(
        DynamicType typeContext,
        EnumUnderlyingType underlyingType,
        Type enumType, Array enumEntries)
    {
        var baseType = typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(enumType);

        var method = typeContext.MethodFactory.Instance.OverrideAction(
            baseType.GetMethod(nameof(SnapshotSerializerValueTypeBase<>.LoadSnapshot),
                [enumType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotReadingScope)])!);

        var argumentTarget = method.Argument(0, enumType.MakeByRefType());
        var argumentNode = method.Argument<SnapshotNode>(1);

        var code = method.Code;

        var variableEnumInteger = method.Variable(
            underlyingType < EnumUnderlyingType.Int64 ? typeof(int) : typeof(long));
        var variableSnapshotValue =
            argumentNode
                .GetPropertyValue(target => target.Value)
                .ToSymbol();

        var labelConverting = method.DefineLabel();

        using (method.If(variableSnapshotValue.IsInstanceOf<StringValue>()))
        {
            var variableEnumString =
                variableSnapshotValue
                    .CastTo<StringValue>()
                    .GetPropertyValue(target => target.Value)
                    .ToSymbol();

            foreach (var enumEntry in enumEntries)
            {
                var labelNext = method.DefineLabel();

                labelNext.GotoIfFalse(
                    variableEnumString.IsEqualTo(enumEntry!.ToString()!));

                variableEnumInteger.AssignContent(
                    underlyingType < EnumUnderlyingType.Int64
                        ? method.Literal(Convert.ToInt32(enumEntry))
                        : method.Literal(Convert.ToInt64(enumEntry))
                );
                labelConverting.Goto();

                labelNext.Mark();
            }
        }

        using (method.If(variableSnapshotValue.IsInstanceOf<Integer32Value>()))
        {
            variableEnumInteger.AssignContent(
                variableSnapshotValue.CastTo<Integer32Value>()
                    .GetPropertyValue(target => target.Value)
            );
            labelConverting.Goto();
        }

        using (method.If(variableSnapshotValue.IsInstanceOf<Integer64Value>()))
        {
            var symbolSnapshotNumber = variableSnapshotValue.CastTo<Integer64Value>()
                .GetPropertyValue(target => target.Value);

            if (underlyingType < EnumUnderlyingType.Int64)
                variableEnumInteger.AssignContent(symbolSnapshotNumber.ToInt32());
            else
                variableEnumInteger.AssignContent(symbolSnapshotNumber);
            labelConverting.Goto();
        }

        // Store the enum value into the target in the form of an integer.
        labelConverting.Mark();
        argumentTarget.LoadContent();
        variableEnumInteger.LoadContent();
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

        method.Return();
    }

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
}