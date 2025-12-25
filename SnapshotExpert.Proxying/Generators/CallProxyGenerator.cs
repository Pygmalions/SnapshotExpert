using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EmitToolbox;
using EmitToolbox.Builders;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Utilities;
using InjectionExpert;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Generator;
using SnapshotExpert.Remoting.Utilities;
using DynamicMethod = EmitToolbox.DynamicMethod;

namespace SnapshotExpert.Remoting.Generators;

/// <summary>
/// This class generates call-proxy classes,
/// which can create delegates that redirect calls into <see cref="ICallProxy"/> instances
/// by serializing arguments and deserializing the result.
/// </summary>
public class CallProxyGenerator
{
    private static readonly CustomAttributeBuilder AttributeRequiredMember =
        new(typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

    private static readonly CustomAttributeBuilder AttributeInjectionMember =
        new(typeof(InjectionAttribute).GetConstructor([typeof(bool)])!, [true]);

    private static readonly CustomAttributeBuilder AttributeSerializerDependency =
        SerializerDependencyAttribute.CreateBuilder(typeof(CallProxyGenerator).FullName!);

    private static readonly DynamicResourceForMethod<CallProxyGenerator> Cache =
        new(Generate, "CallProxyGenerators_");

    private readonly Type _clientType;

    private readonly Type _delegateType;

    private readonly MethodInfo _proxyMethod;

    private CallProxyGenerator(Type delegateType, Type clientType, MethodInfo proxyMethod)
    {
        _delegateType = delegateType;
        _clientType = clientType;
        _proxyMethod = proxyMethod;
    }

    /// <summary>
    /// Get or create the call-proxy generator for the specified delegate type.
    /// </summary>
    /// <param name="targetMethod">Target method to proxy.</param>
    /// <returns>Generator that can generate proxy delegates of the specified delegate type.</returns>
    public static CallProxyGenerator For(MethodInfo targetMethod)
        => Cache[targetMethod];

    /// <summary>
    /// Create a call-proxy delegate with the specified proxy.
    /// </summary>
    /// <param name="serializers">
    /// Serializers for the call-proxy to serialize arguments and deserialize the result.
    /// </param>
    /// <param name="proxy">Call-proxy for the created delegate to use.</param>
    /// <returns>Call-proxy delegate which redirects serialized calls to the specified proxy.</returns>
    public Delegate CreateDelegate(ISerializerProvider serializers, ICallProxy proxy)
    {
        return _proxyMethod.CreateDelegate(_delegateType, Activator
            .CreateInstance(_clientType, proxy)!
            .Autowire(serializers.AsInjections()));
    }

    /// <summary>
    /// Create a call-proxy delegate with the specified proxy.
    /// </summary>
    /// <param name="serializers">
    /// Serializers for the call-proxy to serialize arguments and deserialize the result.
    /// </param>
    /// <param name="proxy">Call-proxy for the created delegate to use.</param>
    /// <typeparam name="TDelegate">Delegate type.</typeparam>
    /// <returns>Call-proxy delegate which redirects serialized calls to the specified proxy.</returns>
    public TDelegate CreateDelegate<TDelegate>(ISerializerProvider serializers, ICallProxy proxy)
        where TDelegate : Delegate
        => (TDelegate)CreateDelegate(serializers, proxy);

    private static CallProxyGenerator Generate(DynamicAssembly assembly, MethodInfo targetMethod)
    {
        var targetType = targetMethod.DelegateType;

        var type = assembly.DefineClass(targetType.GetFriendlyTypeNameForDynamic("CallProxy_"));

        var context = new EmittingContext
        {
            TypeContext = type,
            ProxyField = type.FieldFactory.DefineInstance("Proxy", typeof(ICallProxy))
        };

        // Implement the constructor.
        ImplementConstructor(context);

        // Implement the proxy method.
        var proxyMethod = ImplementProxyMethod(context, targetMethod);

        // Build the type.
        context.TypeContext.Build();

        return new CallProxyGenerator(
            targetType,
            context.TypeContext.BuildingType,
            proxyMethod.BuildingMethod);
    }

    private static DynamicConstructor ImplementConstructor(in EmittingContext context)
    {
        var constructor = context.TypeContext.MethodFactory.Constructor.Define([
            new ParameterDefinition(typeof(ICallProxy), Name: "proxy")
        ]);

        var argumentProxy = constructor.Argument(0, typeof(ICallProxy));
        context.ProxyField
            .SymbolOf(constructor, constructor.This())
            .AssignContent(argumentProxy);
        constructor.Return();

        return constructor;
    }

    private static DynamicMethod ImplementProxyMethod(
        in EmittingContext context, MethodInfo targetMethod)
    {
        var parameterDefinitions = targetMethod.GetParameters()
            .Select(parameter => new ParameterDefinition(parameter.ParameterType))
            .ToArray();
        var hasReturnValue = targetMethod.ReturnType != typeof(void);
        DynamicMethod method = hasReturnValue
            ? context.TypeContext.MethodFactory.Instance.DefineFunctor("Invoke",
                targetMethod.ReturnType, parameterDefinitions, methodModifier: MethodModifier.Virtual)
            : context.TypeContext.MethodFactory.Instance.DefineAction("Invoke",
                parameterDefinitions, methodModifier: MethodModifier.Virtual);
        var asyncBuilder = method.DefineAsyncStateMachine();
        var asyncMethod = asyncBuilder.Method;

        var fieldCallProxy = asyncBuilder.Capture(
            context.ProxyField.SymbolOf<ICallProxy>(method, method.This()));

        var arguments = targetMethod.GetParameters()
            .Select((parameter, index) => (
                Name: parameter.Name ?? index.ToString(),
                Symbol: asyncBuilder.Capture(method.Argument(index, parameter.ParameterType))))
            .ToArray();

        // If the call transporter is null, throw an exception.
        using (asyncMethod.If(fieldCallProxy.IsNull()))
        {
            asyncMethod.ThrowException<NullReferenceException>(
                "This call client is not bound to a call proxy.");
        }

        // Deserialize arguments.
        var variableSerializedArguments = asyncMethod.Variable<ObjectValue>();
        if (arguments.Length > 0)
        {
            variableSerializedArguments.AssignNew();
            foreach (var argument in arguments)
            {
                var symbolArgumentNode = variableSerializedArguments.Invoke(
                    target => target.CreateNode(Any<string>.Value),
                    [asyncMethod.Literal(argument.Name)]);
                var argumentBasicType = argument.Symbol.BasicType;
                var symbolSerializer = asyncBuilder.Capture(
                    context.GetSerializerField(argumentBasicType)
                        .SymbolOf(method, method.This()));
                symbolSerializer.Invoke(
                    typeof(SnapshotSerializer<>).MakeGenericType(argumentBasicType)
                        .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                            [argumentBasicType.MakeByRefType(), typeof(SnapshotNode)])!,
                    [argument.Symbol, symbolArgumentNode]);
            }
        }

        // Perform the call.
        var variableSerializedResult = asyncBuilder.Await(
            fieldCallProxy.Invoke(
                target => target.Call(Any<ObjectValue>.Value),
                [variableSerializedArguments]));

        // If the target method returns void-like types (Task, ValueTask, etc.),
        // complete the builder immediately.
        if (asyncBuilder.ResultType == typeof(void))
        {
            asyncBuilder.Finish(null);

            // If the target method is async:
            if (method.ReturnType == asyncBuilder.TaskType)
            {
                (method as DynamicMethod<Action<ISymbol>>)!.Return(asyncBuilder.Execute());
                return method;
            }

            // If the target method is not async:
            asyncBuilder
                .Execute()
                .Invoke(asyncBuilder.TaskType.GetMethod(nameof(Task.Wait), [])!);
            (method as DynamicMethod<Action>)!.Return();
            return method;
        }

        var variableSnapshotNode = asyncMethod.New(
            () => new SnapshotNode(Any<string>.Value),
            [asyncMethod.Literal("#")]);
        variableSnapshotNode.SetPropertyValue(
            target => target.Value, variableSerializedResult);

        // Deserialize the result.
        var fieldResultSerializer = asyncBuilder.Capture(
            context.GetSerializerField(asyncBuilder.ResultType)
                .SymbolOf(method, method.This()));

        var variableRawResult = asyncMethod.Variable(asyncBuilder.ResultType);

        fieldResultSerializer.Invoke(
            fieldResultSerializer.ContentType.GetMethod(nameof(SnapshotSerializer<>.NewInstance),
                [asyncBuilder.ResultType.MakeByRefType()])!,
            [variableRawResult]);
        fieldResultSerializer.Invoke(
            fieldResultSerializer.ContentType.GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot),
                [asyncBuilder.ResultType.MakeByRefType(), typeof(SnapshotNode)])!,
            [variableRawResult, variableSnapshotNode]);

        asyncBuilder.Finish(variableRawResult);

        if (targetMethod.ReturnType == asyncBuilder.TaskType)
        {
            (method as DynamicMethod<Action<ISymbol>>)!.Return(asyncBuilder.Execute());
            return method;
        }

        var symbolWaitedResult = asyncBuilder
            .Execute()
            .GetPropertyValue(asyncBuilder.TaskType.GetProperty(nameof(Task<>.Result))!);
        (method as DynamicMethod<Action<ISymbol>>)!.Return(symbolWaitedResult);
        return method;
    }

    private readonly struct EmittingContext()
    {
        public required DynamicType TypeContext { get; init; }

        public required DynamicField ProxyField { get; init; }

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
        public static async Task AwaitAsTask(ValueTask<SnapshotValue?> task)
        {
            await task;
        }

        public static async ValueTask AwaitAsValueTask(ValueTask<SnapshotValue?> task)
        {
            await task;
        }

        public static void Wait(ValueTask<SnapshotValue?> task)
        {
            _ = task.Result;
        }

        public static TResult DeserializeResult<TResult>(
            SnapshotSerializer<TResult> serializer, ValueTask<SnapshotValue?> result)
        {
            var task = DeserializeResultAsValueTask(serializer, result);
            return task.IsCompleted ? task.Result : task.AsTask().Result;
        }

        public static async Task<TResult> DeserializeResultAsTask<TResult>(
            SnapshotSerializer<TResult> serializer, ValueTask<SnapshotValue?> result)
        {
            serializer.NewInstance(out var instance);
            serializer.LoadSnapshot(ref instance, new SnapshotNode { Value = await result });
            return instance;
        }

        public static async ValueTask<TResult> DeserializeResultAsValueTask<TResult>(
            SnapshotSerializer<TResult> serializer, ValueTask<SnapshotValue?> result)
        {
            serializer.NewInstance(out var instance);
            serializer.LoadSnapshot(ref instance, new SnapshotNode { Value = await result });
            return instance;
        }
    }
}