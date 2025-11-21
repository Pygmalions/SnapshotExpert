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

internal static class TupleSerializerGenerator
{
    private static readonly DynamicResourceForType<Type> GeneratedSerializerTypes = new(GenerateSerializerType);

    public static Type GetSerializerType(Type enumType) => GeneratedSerializerTypes[enumType];

    public static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type tupleType)
    {
        var baseType = typeof(SnapshotSerializerClassTypeBase<>).MakeGenericType(tupleType);
        var typeContext = assemblyContext.DefineClass(
            tupleType.CreateDynamicFriendlyName("GeneratedTupleSerializer_"), parent: baseType);

        var attributeRequired = new CustomAttributeBuilder(
            typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

        var serializerFields = new Dictionary<Type, DynamicField>();

        var items = tupleType.GetProperties()
            .Select(property =>
            {
                if (serializerFields.TryGetValue(property.PropertyType, out var fieldSerializer))
                    return (ItemProperty: property, SerializerField: fieldSerializer);

                fieldSerializer = typeContext.FieldFactory.DefineInstance(
                    $"ItemSerializer_{property.PropertyType}",
                    typeof(SnapshotSerializer<>).MakeGenericType(property.PropertyType));
                fieldSerializer.MarkAttribute(attributeRequired);
                serializerFields[property.PropertyType] = fieldSerializer;
                return (ItemProperty: property, SerializerField: fieldSerializer);
            })
            .ToArray();

        GenerateSaveSnapshotMethod(typeContext, tupleType, items);
        GenerateLoadSnapshotMethod(typeContext, tupleType, items);
        GenerateSchemaGenerator(typeContext, items);

        typeContext.Build();

        return typeContext.BuildingType;
    }

    private static void GenerateSchemaGenerator(
        DynamicType typeContext, (PropertyInfo ItemProperty, DynamicField SerializerField)[] items)
    {
        var method = typeContext.MethodFactory.Instance.OverrideFunctor(
            typeof(SnapshotSerializer).GetMethod("GenerateSchema",
                BindingFlags.NonPublic | BindingFlags.Instance)!);

        var variableSchemas = method.NewArray<SnapshotSchema>(items.Length);

        // Set the schema for each item.
        foreach (var (index, (_, fieldSerializer)) in items.Index())
        {
            variableSchemas
                .ElementAt(index)
                .AssignValue(fieldSerializer
                    .SymbolOf<SnapshotSerializer>(method, method.This())
                    .GetPropertyValue(target => target.Schema));
        }

        var variableResult = method.New<TupleSchema>();

        variableResult.SetPropertyValue(target => target.Items, variableSchemas);
        variableResult.SetPropertyValue(target => target.Title, method.Value("Tuple"));

        method.Return(variableResult);
    }

    private static void GenerateSaveSnapshotMethod(
        DynamicType typeContext, Type tupleType,
        (PropertyInfo ItemProperty, DynamicField SerializerField)[] items)
    {
        var baseType = typeof(SnapshotSerializerClassTypeBase<>).MakeGenericType(tupleType);
        var method = typeContext.MethodFactory.Instance.OverrideAction(
            baseType.GetMethod("OnSaveSnapshot",
                BindingFlags.Instance | BindingFlags.NonPublic,
                [tupleType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotWritingScope)])!);

        var argumentTarget = method.Argument(0, tupleType.MakeByRefType());
        var argumentNode = method.Argument<SnapshotNode>(1);
        var argumentScope = method.Argument<SnapshotWritingScope>(2);

        var variableArray = method.New<ArrayValue>(
            typeof(ArrayValue).GetConstructor([typeof(int)])!,
            [method.Value(items.Length)]
        );

        foreach (var (propertyItem, fieldSerializer) in items)
        {
            var variableSerializer = fieldSerializer.SymbolOf<SnapshotSerializer>(
                method, method.This());

            var variableItem =
                argumentTarget.Invoke(propertyItem.GetMethod!)!;

            var symbolSubNode = variableArray.Invoke(target => target.CreateNode());

            variableSerializer.Invoke(typeof(SnapshotSerializer<>)
                    .MakeGenericType(propertyItem.PropertyType)
                    .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot), [
                        propertyItem.PropertyType.MakeByRefType(),
                        typeof(SnapshotNode),
                        typeof(SnapshotWritingScope)
                    ])!,
                [variableItem, symbolSubNode, argumentScope]);
        }

        argumentNode.SetPropertyValue(target => target.Value, variableArray);

        method.Return();
    }

    private static void GenerateLoadSnapshotMethod(
        DynamicType typeContext, Type tupleType,
        (PropertyInfo ItemProperty, DynamicField SerializerField)[] items)
    {
        var baseType = typeof(SnapshotSerializerClassTypeBase<>).MakeGenericType(tupleType);
        var method = typeContext.MethodFactory.Instance.OverrideAction(
            baseType.GetMethod("OnLoadSnapshot",
                BindingFlags.Instance | BindingFlags.NonPublic,
                [tupleType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotReadingScope)])!);

        var argumentTarget = method.Argument(0, tupleType.MakeByRefType());
        var argumentNode = method.Argument<SnapshotNode>(1);
        var argumentScope = method.Argument<SnapshotReadingScope>(2);

        var code = method.Code;

        var variableArray = method.Invoke<ArrayValue>(
            typeof(SnapshotConvertibleExtensions)
                .GetMethod(nameof(SnapshotConvertibleExtensions.get_AsArray))!,
            [argumentNode]);

        // Check the length of the snapshot array.
        var labelPassingLengthCheck = method.DefineLabel();
        labelPassingLengthCheck.GotoIfTrue(
            variableArray.Length.IsEqualTo(items.Length));

        // Throw an exception if the length does not match.
        method.New(() => new Exception(Any<string>.Value),
        [
            method.Value(
                $"Failed to load snapshot for ValueTuple '{tupleType}': array has an incorrect length.")
        ]).LoadContent();
        code.Emit(OpCodes.Throw);

        labelPassingLengthCheck.Mark();

        var variableItems = items
            .Select(item => method.Variable(item.ItemProperty.PropertyType))
            .ToArray();

        // Copy the items from the target instance.
        var labelBeginInitialization = method.DefineLabel();
        var labelEndInitialization = method.DefineLabel();

        // If the tuple is null, initialize it.
        argumentTarget.LoadAsValue();
        code.Emit(OpCodes.Brfalse, labelBeginInitialization.Label);

        // Copy items from the existing instance.
        foreach (var (index, (itemProperty, _)) in items.Index())
        {
            variableItems[index].AssignContent(
                argumentTarget.Invoke(itemProperty.GetMethod!)!);
        }

        labelEndInitialization.Goto();

        labelBeginInitialization.Mark();

        // Use item serializers to create new instances for each item.
        foreach (var (index, (itemProperty, fieldSerializer)) in items.Index())
        {
            fieldSerializer.SymbolOf<SnapshotSerializer>(method, method.This())
                .Invoke(typeof(SnapshotSerializer<>)
                        .MakeGenericType(itemProperty.PropertyType)
                        .GetMethod(nameof(SnapshotSerializer<>.NewInstance),
                            [itemProperty.PropertyType.MakeByRefType()])!,
                    [variableItems[index]]);
        }

        labelEndInitialization.Mark();

        foreach (var (index, (propertyItem, fieldSerializer)) in items.Index())
        {
            var symbolSubNode = variableArray.Invoke<SnapshotNode>(
                typeof(ArrayValue).GetMethod("get_Item")!,
                [method.Value(index)]);

            fieldSerializer.SymbolOf<SnapshotSerializer>(method, method.This())
                .Invoke(typeof(SnapshotSerializer<>)
                        .MakeGenericType(propertyItem.PropertyType)
                        .GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot), [
                            propertyItem.PropertyType.MakeByRefType(),
                            typeof(SnapshotNode),
                            typeof(SnapshotReadingScope)
                        ])!,
                    [variableItems[index], symbolSubNode, argumentScope]);
        }

        argumentTarget.AssignNew(
            tupleType.GetConstructor(
                items.Select(item => item.ItemProperty.PropertyType).ToArray())!,
            variableItems);

        method.Return();
    }
}