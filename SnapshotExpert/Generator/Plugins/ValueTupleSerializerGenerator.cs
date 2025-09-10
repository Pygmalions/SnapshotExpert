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

internal static class ValueTupleSerializerGenerator
{
    public static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type tupleType)
    {
        var baseType = typeof(SnapshotSerializerValueTypeBase<>).MakeGenericType(tupleType);
        var typeContext = assemblyContext.DefineClass(
            $"SnapshotSerializer_ValueTuple_{tupleType}", parent: baseType);

        var attributeRequired = new CustomAttributeBuilder(
            typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

        var serializerFields = new Dictionary<Type, FieldBuilder>();
        
        var items = tupleType.GetFields()
            .Select(field =>
            {
                if (serializerFields.TryGetValue(field.FieldType, out var fieldSerializer))
                    return (ItemField: field, SerializerField: fieldSerializer);
                
                fieldSerializer = typeContext.TypeBuilder.DefineField(
                    $"ItemSerializer_{field.FieldType}", 
                    typeof(SnapshotSerializer<>).MakeGenericType(field.FieldType), 
                    FieldAttributes.Public);
                fieldSerializer.SetCustomAttribute(attributeRequired);
                serializerFields[field.FieldType] = fieldSerializer;
                return (ItemField: field, SerializerField: fieldSerializer);
            })
            .ToArray();

        var saverContext = typeContext.ActionBuilder.Override(
            baseType.GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                [tupleType.MakeByRefType(), typeof(SnapshotNode), typeof(SnapshotWritingScope)])!);
        GenerateSaveSnapshotMethod(saverContext, items);
        
        var loaderContext = typeContext.ActionBuilder.Override(
            baseType.GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot),
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
        (FieldInfo ItemField, FieldBuilder SerializerField)[] items)
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
        (FieldInfo ItemField, FieldBuilder SerializerField)[] items)
    {
        var code = methodContext.Code;

        var variableArray = code.DeclareLocal(typeof(ArrayValue));
        code.LoadLiteral(items.Length);
        code.NewObject(typeof(ArrayValue).GetConstructor([typeof(int)])!);
        code.StoreLocal(variableArray);

        foreach (var (fieldItem, fieldSerializer) in items)
        {
            code.LoadArgument_0();
            code.LoadField(fieldSerializer);
            code.LoadArgument_1();
            code.LoadFieldAddress(fieldItem);
            code.LoadLocal(variableArray);
            code.Call(typeof(ArrayValue).GetMethod(nameof(ArrayValue.CreateNode), Type.EmptyTypes)!);
            code.LoadArgument_3();

            code.CallVirtual(typeof(SnapshotSerializer<>)
                .MakeGenericType(fieldItem.FieldType)
                .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot), [
                    fieldItem.FieldType.MakeByRefType(),
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
        (FieldInfo ItemField, FieldBuilder SerializerField)[] items)
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

        foreach (var (index, (fieldItem, fieldSerializer)) in items.Index())
        {
            code.LoadArgument_0();
            code.LoadField(fieldSerializer);
        
            code.LoadArgument_1();
            code.LoadFieldAddress(fieldItem);
        
            code.LoadLocal(variableArray);
            code.LoadLiteral(index);
            code.CallVirtual(typeof(ArrayValue).GetMethod("get_Item")!);
        
            code.LoadArgument_3();
        
            code.CallVirtual(typeof(SnapshotSerializer<>)
                .MakeGenericType(fieldItem.FieldType)
                .GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot), [
                    fieldItem.FieldType.MakeByRefType(),
                    typeof(SnapshotNode),
                    typeof(SnapshotReadingScope)
                ])!);
        }
        
        code.MethodReturn();
    }
}