using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EmitToolbox;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Symbols.Literals;
using EmitToolbox.Utilities;
using InjectionExpert;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Generator;
using SnapshotExpert.Remoting.Utilities;

namespace SnapshotExpert.Remoting.Generators;

public class CallHandlerGenerator
{
    private static readonly CustomAttributeBuilder AttributeRequiredMember =
        new(typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

    private static readonly CustomAttributeBuilder AttributeInjectionMember =
        new(typeof(InjectionAttribute).GetConstructor([typeof(bool)])!, [true]);
    
    private static readonly CustomAttributeBuilder AttributeSerializerDependency =
        SerializerDependencyAttribute.CreateBuilder(typeof(CallHandlerGenerator).FullName!);
    
    private static readonly DynamicResourceForMethod<CallHandlerGenerator> Cache = new(
        Generate, "CallHandlerGenerators_");
    
    /// <summary>
    /// Get or create the call-handler generator for the specified delegate type.
    /// </summary>
    /// <param name="targetMethod">Target method to proxy.</param>
    /// <returns>Generator that can generate call-handlers of the specified delegate type.</returns>
    public static CallHandlerGenerator For(MethodInfo targetMethod)
        => Cache[targetMethod];

    /// <summary>
    /// Create a call-handler for the specified delegate.
    /// </summary>
    /// <param name="serializers"></param>
    /// <param name="targetDelegate">Delegate to redirect the deserialized calls to.</param>
    /// <returns>Handler that can deserialize and redirect calls to the specific delegate.</returns>
    public static ICallHandler For(ISerializerProvider serializers, Delegate targetDelegate)
        => Cache[targetDelegate.Method].CreateHandler(serializers, targetDelegate);
    
    private readonly Type _delegateType;

    private readonly Type _handlerType;

    private CallHandlerGenerator(Type delegateType, Type handlerType)
    {
        _delegateType = delegateType;
        _handlerType = handlerType;
    }

    /// <summary>
    /// Create a call-handler for the specified delegate.
    /// </summary>
    /// <param name="serializers">Serializers for the call-handler to serialize arguments and deserialize the result.</param>
    /// <param name="targetDelegate">Delegate to redirect the deserialized calls to.</param>
    /// <returns>Handler that can deserialize and redirect calls to the specific delegate.</returns>
    public ICallHandler CreateHandler(ISerializerProvider serializers, Delegate targetDelegate)
    {
        if (!_delegateType.IsInstanceOfType(targetDelegate))
            throw new ArgumentException($"Specific delegate is not of the delegate type '{_delegateType}'.");
        return (ICallHandler)Activator
            .CreateInstance(_handlerType, targetDelegate)!
            .Autowire(serializers.AsInjections());
    }

    private static CallHandlerGenerator Generate(DynamicAssembly assembly, MethodInfo targetMethod)
    {
        var targetType = targetMethod.DelegateType;
       
        var typeContext = assembly
            .DefineClass(targetType.GetFriendlyTypeNameForDynamic("CallServer_"));
        
        var context = new EmittingContext
        {
            TargetType = targetType,
            TypeContext = typeContext,
            DelegateField = typeContext.FieldFactory.DefineInstance("Delegate", targetType)
        };

        ImplementConstructor(context);
        ImplementHandlerMethod(context, targetMethod);

        typeContext.Build();
        
        return new CallHandlerGenerator(targetType, typeContext.BuildingType);
    }

    private static DynamicConstructor ImplementConstructor(in EmittingContext context)
    {
        var constructor = context.TypeContext.MethodFactory.Constructor.Define([
            new ParameterDefinition(context.TargetType, Name: "delegate")
        ]);

        var argumentDelegate = constructor.Argument(0, context.TargetType);
        context.DelegateField
            .SymbolOf(constructor, constructor.This())
            .AssignContent(argumentDelegate);
        constructor.Return();

        return constructor;
    }
    
    private static DynamicFunction ImplementHandlerMethod(
        in EmittingContext context, MethodInfo targetMethod)
    {
        context.TypeContext.ImplementInterface(typeof(ICallHandler));
        
        var method = context.TypeContext.MethodFactory.Instance.OverrideFunctor<SnapshotValue?>(
            typeof(ICallHandler).GetMethod(nameof(ICallHandler.HandleCall))!);
        
        var argumentSerializedArguments = method.Argument<ObjectValue>(0);

        var variablesRawArgument = targetMethod.GetParameters()
            .Select(parameter => method.Variable(parameter.ParameterType))
            .ToArray();

        // Deserialize arguments.
        foreach (var (index, parameter) in targetMethod.GetParameters().Index())
        {
            var variableArgumentNode = argumentSerializedArguments.Invoke(
                target => target.GetDeclaredNode(Any<string>.Value),
                [method.Value(parameter.Name ?? index.ToString())]);

            var labelContinue = method.DefineLabel();

            using (method.If(variableArgumentNode.IsNull()))
            {
                if (!parameter.HasDefaultValue)
                {
                    method.ThrowException<ArgumentException>(
                        $"Cannot find argument '{parameter.Name ?? index.ToString()}'");
                }
                else
                {
                    variablesRawArgument[index].AssignContent(
                        parameter.DefaultValue is null
                            ? method.Null<object>()
                            : LiteralSymbolFactory.Create(method, parameter.DefaultValue));
                }
                labelContinue.Goto();
            }

            var parameterBasicType = parameter.ParameterType.BasicType;
            var symbolSerializer = context.GetSerializerField(parameterBasicType)
                .SymbolOf(method, method.This());
            var serializerType = typeof(SnapshotSerializer<>).MakeGenericType(parameterBasicType);
            symbolSerializer.Invoke(
                serializerType.GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot),
                    [parameterBasicType.MakeByRefType(), typeof(SnapshotNode)])!,
                [
                    variablesRawArgument[index],
                    variableArgumentNode
                ]
            );

            labelContinue.Mark();
        }

        // Invoke method.
        var hasReturnValue = targetMethod.ReturnType != typeof(void);

        var variableResult = context.DelegateField
            .SymbolOf(method, method.This())
            // The 'targetMethod' is only for getting parameter names.
            // Here, 'Invoke(...)' of the delegate is called;
            // otherwise the delegate instance will be falsely considered as the instance
            // of the declaring type of 'targetMethod'.
            .Invoke(context.TargetType.GetMethod("Invoke")!, variablesRawArgument);

        if (!hasReturnValue)
        {
            method.Return(method.Null<SnapshotValue>());
        }
        else
        {
            var variableResultNode = method.New(
                () => new SnapshotNode(Any<string>.Value), [method.Value("#")]);

            // Serialize result.
            var fieldResultSerializer = context.GetSerializerField(targetMethod.ReturnType)
                .SymbolOf(method, method.This());
            fieldResultSerializer.Invoke(
                typeof(SnapshotSerializer<>).MakeGenericType(targetMethod.ReturnType)
                    .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                        [targetMethod.ReturnType.MakeByRefType(), typeof(SnapshotNode)])!,
                [variableResult!, variableResultNode]
            );
            method.Return(variableResultNode.GetPropertyValue(target => target.Value));
        }

        return method;
    }

    private readonly struct EmittingContext()
    {
        public required DynamicType TypeContext { get; init; }

        public required Type TargetType { get; init; }
        
        public required DynamicField DelegateField { get; init; }

        private readonly Dictionary<Type, DynamicField> _serializers = new();

        public DynamicField GetSerializerField(Type targetType)
        {
            ref var targetField = ref CollectionsMarshal.GetValueRefOrAddDefault(
                _serializers, targetType, out var existing);
            if (existing)
                return targetField!;

            targetField = TypeContext.FieldFactory.DefineInstance(
                $"Serializer_{targetType.GetFriendlyTypeNameForDynamic()}",
                typeof(SnapshotSerializer<>).MakeGenericType(targetType)
            );
            targetField
                .MarkAttribute(AttributeInjectionMember)
                .MarkAttribute(AttributeRequiredMember)
                .MarkAttribute(AttributeSerializerDependency);
            return targetField;
        }
    }
}