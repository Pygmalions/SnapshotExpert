using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EmitToolbox;
using EmitToolbox.Builders;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Symbols.Literals;
using EmitToolbox.Utilities;
using InjectionExpert;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Data.Values.Primitives;
using SnapshotExpert.Generator;
using SnapshotExpert.Remoting.Utilities;

namespace SnapshotExpert.Remoting.Generators;

/// <summary>
/// This class generates call-handler classes,
/// which can redirect serialized calls to specific delegates
/// by deserializing arguments and serializing the result.
/// </summary>
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

    private readonly Type _handlerType;

    private readonly Type _methodType;

    private CallHandlerGenerator(Type methodType, Type handlerType)
    {
        _methodType = methodType;
        _handlerType = handlerType;
    }

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

    /// <summary>
    /// Create a call-handler for the specified delegate.
    /// </summary>
    /// <param name="serializers">Serializers for the call-handler to serialize arguments and deserialize the result.</param>
    /// <param name="targetDelegate">Delegate to redirect the deserialized calls to.</param>
    /// <returns>Handler that can deserialize and redirect calls to the specific delegate.</returns>
    public ICallHandler CreateHandler(ISerializerProvider serializers, Delegate targetDelegate)
    {
        if (!_methodType.IsInstanceOfType(targetDelegate))
            throw new ArgumentException($"Specific delegate is not of the delegate type '{_methodType}'.");
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
            DelegateType = targetType,
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
            new ParameterDefinition(context.DelegateType, Name: "delegate")
        ]);

        var argumentDelegate = constructor.Argument(0, context.DelegateType);
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

        var method = context.TypeContext.MethodFactory.Instance.OverrideFunctor<ValueTask<SnapshotValue?>>(
            typeof(ICallHandler).GetMethod(nameof(ICallHandler.HandleCall))!);

        var asyncBuilder = method.DefineAsyncStateMachine();
        var asyncMethod = asyncBuilder.Method;

        var fieldSerializedArguments = asyncBuilder.Capture(
            method.Argument<ObjectValue>(0));

        var variablesRawArgument = targetMethod.GetParameters()
            .Select(parameter => asyncMethod.Variable(parameter.ParameterType))
            .ToArray();

        // Deserialize arguments.
        foreach (var (index, parameter) in targetMethod.GetParameters().Index())
        {
            var variableArgumentNode = fieldSerializedArguments.Invoke(
                target => target.GetNode(Any<string>.Value),
                [asyncMethod.Literal(parameter.Name ?? index.ToString())]);

            var labelContinue = asyncMethod.DefineLabel();

            using (asyncMethod.If(variableArgumentNode.IsNull()))
            {
                if (!parameter.HasDefaultValue)
                {
                    asyncMethod.ThrowException<ArgumentException>(
                        $"Cannot find argument '{parameter.Name ?? index.ToString()}'");
                }
                else
                {
                    variablesRawArgument[index].AssignContent(
                        parameter.DefaultValue is null
                            ? asyncMethod.Null<object>()
                            : LiteralSymbolFactory.Create(asyncMethod, parameter.DefaultValue));
                }

                labelContinue.Goto();
            }

            var parameterBasicType = parameter.ParameterType.BasicType;
            var symbolSerializer =
                asyncBuilder.Capture(context.GetSerializerField(parameterBasicType)
                    .SymbolOf(method, method.This()));
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
        ISymbol? variableRawResult = asyncBuilder
            .Capture(context.DelegateField.SymbolOf(method, method.This()))
            .Invoke(context.DelegateType.GetMethod("Invoke")!, variablesRawArgument);

        var resultType = targetMethod.ReturnType;

        if (resultType == typeof(void))
        {
            asyncBuilder.Finish(asyncMethod.Null<SnapshotValue>());
            method.Return(asyncBuilder.Execute().AsSymbol<ValueTask<SnapshotValue?>>());
            return method;
        }

        if (resultType == typeof(Task) || resultType == typeof(ValueTask))
        {
            asyncBuilder.Await(variableRawResult!);
            asyncBuilder.Finish(asyncMethod.Null<SnapshotValue>());
            method.Return(asyncBuilder.Execute().AsSymbol<ValueTask<SnapshotValue?>>());
            return method;
        }

        var resultDefinition = resultType.IsGenericType ? resultType.GetGenericTypeDefinition() : null;

        if (resultDefinition == typeof(Task<>) || resultDefinition == typeof(ValueTask<>))
        {
            resultType = resultType.GetGenericArguments()[0];
            variableRawResult = asyncBuilder.Await(variableRawResult!)!;
        }

        var variableResultNode = asyncMethod.New(
            () => new SnapshotNode(Any<string>.Value), [asyncMethod.Literal("#")]);

        // Serialize result.
        var fieldResultSerializer = asyncBuilder.Capture(
            context.GetSerializerField(resultType).SymbolOf(method, method.This()));

        fieldResultSerializer.Invoke(
            typeof(SnapshotSerializer<>).MakeGenericType(resultType)
                .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                    [resultType.MakeByRefType(), typeof(SnapshotNode)])!,
            [variableRawResult!, variableResultNode]
        );
        asyncBuilder.Finish(variableResultNode.GetPropertyValue(target => target.Value));
        method.Return(asyncBuilder.Execute().AsSymbol<ValueTask<SnapshotValue?>>());

        return method;
    }

    private readonly struct EmittingContext()
    {
        public required DynamicType TypeContext { get; init; }

        public required Type DelegateType { get; init; }

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

    public static class Utilities
    {
        public static ValueTask<SnapshotValue> SerializeResult<TResult>(
            SnapshotSerializer<TResult> serializer, TResult? result)
        {
            var node = new SnapshotNode();
            if (result is null)
                node.Value = new NullValue();
            else
                serializer.SaveSnapshot(result, node);
            return ValueTask.FromResult(node.Value!);
        }

        public static async ValueTask<SnapshotValue?> SerializeTask<TResult>(
            SnapshotSerializer<TResult> serializer, Task<TResult?> task)
        {
            var node = new SnapshotNode();
            var result = await task;
            if (result is null)
                node.Value = new NullValue();
            else
                serializer.SaveSnapshot(result, node);
            return node.Value!;
        }

        public static async ValueTask<SnapshotValue> SerializeValueTask<TResult>(
            SnapshotSerializer<TResult> serializer, ValueTask<TResult?> task)
        {
            var node = new SnapshotNode();
            var result = await task;
            if (result is null)
                node.Value = new NullValue();
            else
                serializer.SaveSnapshot(result, node);
            return node.Value!;
        }
    }
}