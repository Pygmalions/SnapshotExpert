using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EmitToolbox;
using EmitToolbox.Extensions;
using EmitToolbox.Framework;
using InjectionExpert;
using SnapshotExpert.Generator.Plugins;
using SnapshotExpert.Serializers;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Generator;

public static partial class SerializerGenerator
{
    private static readonly DynamicResourceCacheForType<Type> Cache = new(
        GenerateSerializerType, "GeneratedSerializers_");

    private static readonly CustomAttributeBuilder AttributeRequiredMember =
        new(typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

    private static readonly CustomAttributeBuilder AttributeInjectionMember =
        new(typeof(InjectionAttribute).GetConstructor([typeof(bool)])!, [true]);

    public static Type For(Type type) => Cache[type];

    private static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type targetType)
    {
        if (targetType.IsEnum)
            return EnumSerializerGenerator.GenerateSerializerType(assemblyContext, targetType);
        if (targetType.IsArray && targetType.GetArrayRank() > 1)
            return MatrixSerializerGenerator.GenerateSerializerType(assemblyContext, targetType);
        if (targetType.IsAssignableTo(typeof(ITuple)))
        {
            return targetType.IsValueType
                ? ValueTupleSerializerGenerator.GenerateSerializerType(assemblyContext, targetType)
                : TupleSerializerGenerator.GenerateSerializerType(assemblyContext, targetType);
        }

        var baseType = (targetType.IsValueType
                ? typeof(SnapshotSerializerValueTypeBase<>)
                : typeof(SnapshotSerializerClassTypeBase<>))
            .MakeGenericType(targetType);
        var typeContext = assemblyContext.DefineClass(
            "GeneratedSerializer_" + targetType, parent: baseType);

        var classContext = new ClassContext
        {
            TargetType = targetType,
            TypeContext = typeContext,
            SerializerBaseType = baseType,
        };

        ImplementInstantiateMethod(classContext);

        var loaderContext = new LoaderMethodBuilder();
        loaderContext.Initialize(classContext);

        var saverContext = new SaverMethodBuilder();
        saverContext.Initialize(classContext);

        var schemaContext = new SchemaMethodBuilder();
        schemaContext.Initialize(classContext);

        IterateTypeHierarchy(targetType);

        loaderContext.Complete();
        saverContext.Complete();
        schemaContext.Complete();

        typeContext.Build();

        return typeContext.BuildingType;

        void IterateTypeHierarchy(Type currentType)
        {
            // Serializer members of deeper base types first.
            var currentBaseType = currentType.BaseType;
            if (currentBaseType != null &&
                currentBaseType != typeof(object) &&
                currentBaseType != typeof(ValueType))
                IterateTypeHierarchy(currentBaseType);
            
            // Filter fields to serialize.
            foreach (var field in currentType.GetFields(
                         BindingFlags.Instance | BindingFlags.DeclaredOnly |
                         BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.GetCustomAttribute<TransientAttribute>(true) != null)
                    continue;

                MemberInfo member = field;

                if (field.GetFrontingProperty() is { } property)
                {
                    member = property;
                    if (property.GetCustomAttribute<TransientAttribute>(true) != null)
                        continue;
                }

                loaderContext.Generate(field, member);
                saverContext.Generate(field, member);
                schemaContext.Generate(field, member);
            }
        }
    }

    private static void ImplementInstantiateMethod(ClassContext context)
    {
        var method = context.TypeContext.ActionBuilder.Override(
            typeof(SnapshotSerializer<>)
                .MakeGenericType(context.TargetType)
                .GetMethod(nameof(SnapshotSerializer<>.NewInstance),
                    [context.TargetType.MakeByRefType()])!);

        var code = method.Code;

        var variableThis = code.DeclareLocal(context.TargetType);

        var constructor = context.TargetType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            Type.EmptyTypes);

        if (constructor == null)
        {
            // Create an uninitialized object of the target type.
            if (!context.TargetType.IsValueType)
            {
                code.LoadTypeInfo(context.TargetType);
                code.Call(typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))!);
                code.StoreLocal(variableThis);
            }

            // Invoke the nearest parameterless constructor.
            for (var ancestorType = context.TargetType;
                 ancestorType != null && ancestorType != typeof(object) && ancestorType != typeof(ValueType);
                 ancestorType = ancestorType.BaseType)
            {
                var ancestorConstructor = ancestorType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    Type.EmptyTypes);
                if (ancestorConstructor == null)
                    continue;
                if (context.TargetType.IsValueType)
                    code.LoadLocalAddress(variableThis);
                else
                    code.LoadLocal(variableThis);
                code.Call(ancestorConstructor);
                break;
            }
        }
        else
        {
            code.NewObject(constructor);
            code.StoreLocal(variableThis);
        }

        code.LoadArgument_1();
        code.LoadLocal(variableThis);
        if (context.TargetType.IsValueType)
            code.Emit(OpCodes.Stobj, context.TargetType);
        else
            code.Emit(OpCodes.Stind_Ref);

        code.MethodReturn();
    }
}