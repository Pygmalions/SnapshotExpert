using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EmitToolbox;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Utilities;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Schemas.Primitives;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Serializers.Primitives.Generators;

internal static class ValueTupleSerializerGenerator
{
    private static readonly DynamicResourceForType<Type> GeneratedSerializerTypes = new(GenerateSerializerType);

    public static Type GetSerializerType(Type enumType) => GeneratedSerializerTypes[enumType];

    public static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type tupleType)
    {
        var baseType = typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(tupleType);
        var typeContext = assemblyContext.DefineClass(
            tupleType.CreateDynamicFriendlyName("GeneratedValueTupleSerializer_"), parent: baseType);

        var attributeRequired = new CustomAttributeBuilder(
            typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

        var serializerFields = new Dictionary<Type, DynamicField>();

        var items = tupleType.GetFields()
            .Select(field =>
            {
                if (serializerFields.TryGetValue(field.FieldType, out var fieldSerializer))
                    return (ItemField: field, SerializerField: fieldSerializer);

                fieldSerializer = typeContext.FieldFactory.DefineInstance(
                    $"ItemSerializer_{field.FieldType}",
                    typeof(SnapshotSerializer<>).MakeGenericType(field.FieldType));
                fieldSerializer.MarkAttribute(attributeRequired);
                serializerFields[field.FieldType] = fieldSerializer;
                return (ItemField: field, SerializerField: fieldSerializer);
            })
            .ToArray();

        GenerateSaveSnapshotMethod(typeContext, tupleType, items);
        GenerateLoadSnapshotMethod(typeContext, tupleType, items);
        GenerateSchemaGenerator(typeContext, items);

        typeContext.Build();

        return typeContext.BuildingType;
    }

    private static void GenerateSchemaGenerator(
        DynamicType typeContext,
        (FieldInfo ItemField, DynamicField SerializerField)[] items)
    {
        var method = typeContext.MethodFactory.Instance.OverrideFunctor(
            typeof(SnapshotSerializer).GetMethod("GenerateSchema",
                BindingFlags.NonPublic | BindingFlags.Instance)!);

        var variableSchemas =
            method.NewArray<SnapshotSchema>(items.Length);

        // Set the schema for each item.
        foreach (var (index, (_, fieldSerializer)) in items.Index())
        {
            var symbolSerializer = fieldSerializer.SymbolOf<SnapshotSerializer>(method, method.This());

            variableSchemas
                .ElementAt(index)
                .AssignContent(symbolSerializer.GetPropertyValue(target => target.Schema));
        }

        var variableResult = method.New<TupleSchema>();

        variableResult.SetPropertyValue(target => target.Items, variableSchemas);
        variableResult.SetPropertyValue(target => target.Title, method.Value("Tuple"));

        method.Return(variableResult);
    }

    private static void GenerateSaveSnapshotMethod(
        DynamicType typeContext, Type tupleType,
        (FieldInfo ItemField, DynamicField SerializerField)[] items)
    {
        var baseType = typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(tupleType);
        var method = typeContext.MethodFactory.Instance.OverrideAction(
            baseType.GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                [tupleType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotWritingScope)])!);

        var argumentTarget = method.Argument(0, tupleType.MakeByRefType());
        var argumentNode = method.Argument<SnapshotNode>(1);
        var argumentScope = method.Argument<SnapshotWritingScope>(2);

        var variableArray = method.New(
            () => new ArrayValue(Any<int>.Value),
            [method.Value(items.Length)]);

        foreach (var (fieldItem, fieldSerializer) in items)
        {
            var symbolSerializer = fieldSerializer.SymbolOf<SnapshotSerializer>(method, method.This());

            var symbolField = new FieldSymbol(method, fieldItem, argumentTarget);
            var variableSubNode =
                variableArray.Invoke(target => target.CreateNode());

            symbolSerializer.Invoke(typeof(SnapshotSerializer<>)
                    .MakeGenericType(fieldItem.FieldType)
                    .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot), [
                        fieldItem.FieldType.MakeByRefType(),
                        typeof(SnapshotNode),
                        typeof(SnapshotWritingScope)
                    ])!,
                [symbolField, variableSubNode, argumentScope]);
        }

        argumentNode.SetPropertyValue(target => target.Value, variableArray);

        method.Return();
    }

    private static void GenerateLoadSnapshotMethod(
        DynamicType typeContext, Type tupleType,
        (FieldInfo ItemField, DynamicField SerializerField)[] items)
    {
        var baseType = typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(tupleType);
        var method = typeContext.MethodFactory.Instance.OverrideAction(
            baseType.GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot),
                [tupleType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotReadingScope)])!);

        var argumentTarget = method.Argument(0, tupleType.MakeByRefType());
        var argumentNode = method.Argument<SnapshotNode>(1);
        var argumentScope = method.Argument<SnapshotReadingScope>(2);
        
        var code = method.Code;

        var variableArray = method.Invoke<ArrayValue>(
            typeof(SnapshotNodeExtensions)
                .GetMethod(nameof(SnapshotNodeExtensions.RequireValue))!
                .MakeGenericMethod(typeof(ArrayValue)),
            [argumentNode]
        );
        
        // Check the length of the snapshot array.
        var labelPassingLengthCheck = method.DefineLabel();
        labelPassingLengthCheck.GotoIfTrue(
            variableArray.Length.IsEqualTo(items.Length));

        // Throw an exception if the length does not match.
        method.New(() => new Exception(Any<string>.Value),
        [
            method.Value($"Failed to load snapshot for ValueTuple '{tupleType}': array has an incorrect length.")
        ]).LoadContent();
        code.Emit(OpCodes.Throw);

        labelPassingLengthCheck.Mark();

        foreach (var (index, (fieldItem, fieldSerializer)) in items.Index())
        {
            var symbolSerializer = fieldSerializer.SymbolOf<SnapshotSerializer>(method, method.This());
            
            var symbolField = new FieldSymbol(method, fieldItem, argumentTarget);

            var variableSubNode = variableArray.Invoke<SnapshotNode>(
                typeof(ArrayValue).GetMethod("get_Item")!,
                [method.Value(index)]);

            symbolSerializer.Invoke(typeof(SnapshotSerializer<>)
                .MakeGenericType(fieldItem.FieldType)
                .GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot), [
                    fieldItem.FieldType.MakeByRefType(),
                    typeof(SnapshotNode),
                    typeof(SnapshotReadingScope)
                ])!,
                [
                    symbolField, variableSubNode, argumentScope
                ]);
        }

        method.Return();
    }
}