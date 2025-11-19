using System.Reflection;
using System.Runtime.CompilerServices;
using EmitToolbox;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Utilities;
using SnapshotExpert.Serializers;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Generator;

public static partial class SerializerGenerator
{
    private static readonly DynamicResourceForType<Type> Cache = new(
        GenerateSerializerType, "GeneratedSerializers_");

    public static Type For(Type type) => Cache[type];

    private static Type GenerateSerializerType(DynamicAssembly assemblyContext, Type targetType)
    {
        var baseType = (targetType.IsValueType
                ? typeof(SnapshotSerializerValueTypeBase<>)
                : typeof(SnapshotSerializerClassTypeBase<>))
            .MakeGenericType(targetType);
        var typeContext = assemblyContext.DefineClass(
            targetType.CreateDynamicFriendlyName("GeneratedSerializer_"), parent: baseType);

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
        var method =
            context.TypeContext.MethodFactory.Instance.OverrideAction(
                typeof(SnapshotSerializer<>)
                    .MakeGenericType(context.TargetType)
                    .GetMethod(nameof(SnapshotSerializer<>.NewInstance),
                        [context.TargetType.MakeByRefType()])!);

        var argumentInstance = method.Argument(
            0, context.TargetType.MakeByRefType());

        var constructor = context.TargetType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            Type.EmptyTypes);

        var variableInstance = method.Variable(context.TargetType);

        if (constructor != null)
        {
            variableInstance.AssignNew(constructor);
        }
        else
        {
            // Create an uninitialized object of the target type.
            if (!context.TargetType.IsValueType)
            {
                method
                    .Invoke(
                        () => RuntimeHelpers.GetUninitializedObject(Any<Type>.Value),
                        [method.Value(context.TargetType)])
                    .ToSymbol(variableInstance);
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
                variableInstance.Invoke(ancestorConstructor);
                break;
            }
        }
        
        argumentInstance.CopyValueFrom(variableInstance);

        method.Return();
    }
}