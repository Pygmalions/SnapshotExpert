using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EmitToolbox.Extensions;
using EmitToolbox.Framework;
using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Schemas.Primitives;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Generator.Plugins;

internal static class TupleSerializerGenerator
{
    public static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type tupleType)
    {
        var baseType = typeof(SnapshotSerializerClassTypeBase<>).MakeGenericType(tupleType);
        var typeContext = assemblyContext.DefineClass(
            $"SnapshotSerializer_Tuple_{tupleType}", parent: baseType);

        var attributeRequired = new CustomAttributeBuilder(
            typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

        var serializerFields = new Dictionary<Type, FieldBuilder>();

        var items = tupleType.GetProperties()
            .Select(property =>
            {
                if (serializerFields.TryGetValue(property.PropertyType, out var fieldSerializer))
                    return (ItemProperty: property, SerializerField: fieldSerializer);

                fieldSerializer = typeContext.TypeBuilder.DefineField(
                    $"ItemSerializer_{property.PropertyType}",
                    typeof(SnapshotSerializer<>).MakeGenericType(property.PropertyType),
                    FieldAttributes.Public);
                fieldSerializer.SetCustomAttribute(attributeRequired);
                serializerFields[property.PropertyType] = fieldSerializer;
                return (ItemProperty: property, SerializerField: fieldSerializer);
            })
            .ToArray();

        var saverContext = typeContext.ActionBuilder.Override(
            baseType.GetMethod("OnSaveSnapshot",
                BindingFlags.Instance | BindingFlags.NonPublic,
                [tupleType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotWritingScope)])!);
        GenerateSaveSnapshotMethod(saverContext, items);

        var loaderContext = typeContext.ActionBuilder.Override(
            baseType.GetMethod("OnLoadSnapshot",
                BindingFlags.Instance | BindingFlags.NonPublic,
                [tupleType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotReadingScope)])!);
        GenerateLoadSnapshotMethod(loaderContext, tupleType, items);
        
        var schemaGeneratorContext = typeContext.FunctorBuilder.Override(
            typeof(SnapshotSerializer).GetMethod("GenerateSchema", 
                BindingFlags.NonPublic | BindingFlags.Instance)!);
        GenerateSchemaGenerator(schemaGeneratorContext, items);

        typeContext.Build();

        return typeContext.BuildingType;
    }
    
    private static void GenerateSchemaGenerator(DynamicFunctor methodContext, 
        (PropertyInfo ItemProperty, FieldBuilder SerializerField)[] items)
    {
        var code = methodContext.Code;

        var variableSchemas = code.DeclareLocal(typeof(SnapshotSchema[]));
        code.LoadLiteral(items.Length);
        code.NewArray(typeof(SnapshotSchema));
        code.StoreLocal(variableSchemas);

        // Set schema for each item.
        foreach (var (index, (_, fieldSerializer)) in items.Index())
        {
            code.LoadLocal(variableSchemas);
            code.LoadLiteral(index);
            
            code.LoadArgument_0();
            code.LoadField(fieldSerializer);
            code.LoadProperty(typeof(SnapshotSerializer).GetProperty(nameof(SnapshotSerializer.Schema))!);
            
            code.StoreArrayElement_Class();
        }
        
        var variableResult = code.DeclareLocal(typeof(SnapshotSchema));
        code.NewObject(typeof(TupleSchema).GetConstructor(Type.EmptyTypes)!);
        code.StoreLocal(variableResult);
        
        code.LoadLocal(variableResult);
        code.LoadLocal(variableSchemas);
        code.StoreProperty(typeof(TupleSchema).GetProperty(nameof(TupleSchema.Items))!);
        
        code.LoadLocal(variableResult);
        code.LoadLiteral("Tuple");
        code.StoreProperty(typeof(SnapshotSchema).GetProperty(nameof(SnapshotSchema.Title))!);
        
        code.LoadLocal(variableResult);
        code.MethodReturn();
    }
    
    private static void GenerateSaveSnapshotMethod(
        DynamicAction methodContext,
        (PropertyInfo ItemProperty, FieldBuilder SerializerField)[] items)
    {
        var code = methodContext.Code;

        var variableArray = code.DeclareLocal(typeof(ArrayValue));
        code.LoadLiteral(items.Length);
        code.NewObject(typeof(ArrayValue).GetConstructor([typeof(int)])!);
        code.StoreLocal(variableArray);

        foreach (var (propertyItem, fieldSerializer) in items)
        {
            code.LoadArgument_0();
            code.LoadField(fieldSerializer);

            var variableItem = code.DeclareLocal(propertyItem.PropertyType);
            code.LoadArgument_1();
            code.Emit(OpCodes.Ldind_Ref);
            code.LoadProperty(propertyItem);
            code.StoreLocal(variableItem);
            code.LoadLocalAddress(variableItem);

            code.LoadLocal(variableArray);
            code.Call(typeof(ArrayValue).GetMethod(nameof(ArrayValue.CreateNode), Type.EmptyTypes)!);

            code.LoadArgument_3();

            code.CallVirtual(typeof(SnapshotSerializer<>)
                .MakeGenericType(propertyItem.PropertyType)
                .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot), [
                    propertyItem.PropertyType.MakeByRefType(),
                    typeof(SnapshotNode),
                    typeof(SnapshotWritingScope)
                ])!);
        }

        code.LoadArgument_2();
        code.LoadLocal(variableArray);
        code.StoreProperty(typeof(SnapshotNode).GetProperty(nameof(SnapshotNode.Value))!);

        code.MethodReturn();
    }

    private static void GenerateLoadSnapshotMethod(
        DynamicAction methodContext, Type tupleType,
        (PropertyInfo ItemProperty, FieldBuilder SerializerField)[] items)
    {
        var code = methodContext.Code;

        var variableArray = code.DeclareLocal(typeof(ArrayValue));
        code.LoadArgument_2();
        code.Call(typeof(SnapshotNodeExtensions)
            .GetMethod(nameof(SnapshotNodeExtensions.RequireValue))!
            .MakeGenericMethod(typeof(ArrayValue)));
        code.StoreLocal(variableArray);

        // Check the length of the snapshot array.
        {
            code.LoadLocal(variableArray);
            code.LoadProperty(typeof(ArrayValue).GetProperty(nameof(ArrayValue.Count))!);
            code.LoadLiteral(items.Length);
            var labelHasCorrectLength = code.DefineLabel();
            code.GotoIfEqual(labelHasCorrectLength);

            // Throw an exception if the length does not match.
            code.LoadLiteral(
                $"Failed to load snapshot for ValueTuple '{tupleType}': array has an incorrect length.");
            code.NewObject(typeof(Exception).GetConstructor([typeof(string)])!);
            code.Emit(OpCodes.Throw);

            code.MarkLabel(labelHasCorrectLength);
        }

        var variableItems = items
            .Select(item => code.DeclareLocal(item.ItemProperty.PropertyType))
            .ToArray();

        // Copy item from the target instance.
        var labelBeginInitialization = code.DefineLabel();
        var labelEndInitialization = code.DefineLabel();

        // If the tuple is null, initialize it.
        code.LoadArgument_1();
        code.Emit(OpCodes.Ldind_Ref);
        code.GotoIfFalse(labelBeginInitialization);

        // Copy items from the existing instance.
        foreach (var (index, (itemProperty, _)) in items.Index())
        {
            code.LoadArgument_1();
            code.Emit(OpCodes.Ldind_Ref);
            code.LoadProperty(itemProperty);
            code.StoreLocal(variableItems[index]);
        }

        code.Goto(labelEndInitialization);

        code.MarkLabel(labelBeginInitialization);

        // Use item serializers to create new instances for each item.
        foreach (var (index, (itemProperty, fieldSerializer)) in items.Index())
        {
            code.LoadArgument_0();
            code.LoadField(fieldSerializer);
            code.LoadLocalAddress(variableItems[index]);
            code.CallVirtual(typeof(SnapshotSerializer<>)
                .MakeGenericType(itemProperty.PropertyType)
                .GetMethod(nameof(SnapshotSerializer<>.NewInstance), [
                    itemProperty.PropertyType.MakeByRefType(),
                ])!);
        }

        code.MarkLabel(labelEndInitialization);

        foreach (var (index, (propertyItem, fieldSerializer)) in items.Index())
        {
            code.LoadArgument_0();
            code.LoadField(fieldSerializer);

            code.LoadLocalAddress(variableItems[index]);

            code.LoadLocal(variableArray);
            code.LoadLiteral(index);
            code.CallVirtual(typeof(ArrayValue).GetMethod("get_Item")!);

            code.LoadArgument_3();

            code.CallVirtual(typeof(SnapshotSerializer<>)
                .MakeGenericType(propertyItem.PropertyType)
                .GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot), [
                    propertyItem.PropertyType.MakeByRefType(),
                    typeof(SnapshotNode),
                    typeof(SnapshotReadingScope)
                ])!);
        }

        code.LoadArgument_1();
        foreach (var variableItem in variableItems)
            code.LoadLocal(variableItem);
        code.NewObject(tupleType.GetConstructor(
            items.Select(item => item.ItemProperty.PropertyType).ToArray())!);
        code.Emit(OpCodes.Stind_Ref);

        code.MethodReturn();
    }
}