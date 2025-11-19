using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EmitToolbox;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Utilities;
using HarmonyLib;
using InjectionExpert;
using InjectionExpert.Injectors;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Generator;
using DynamicMethod = EmitToolbox.DynamicMethod;

namespace SnapshotExpert.Remoting.Generators;

public static class CallClientGenerator
{
    private static readonly CustomAttributeBuilder AttributeRequiredMember =
        new(typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

    private static readonly CustomAttributeBuilder AttributeInjectionMember =
        new(typeof(InjectionAttribute).GetConstructor([typeof(bool)])!, [true]);

    private static readonly CustomAttributeBuilder AttributeSerializerDependency =
        SerializerDependencyAttribute.CreateBuilder(typeof(CallClientGenerator).FullName!);

    private static readonly DynamicResourceForType<Type> Cache = new(GenerateClient,
        "CallClients_");

    private static readonly Harmony Patcher = new(typeof(CallClientGenerator).FullName);

    private static readonly AsyncLocal<MethodBase> CurrentProxyMethod = new();

    /// <summary>
    /// Generate a call client type for the specified type.
    /// </summary>
    /// <param name="type">Type to handle.</param>
    /// <returns>
    /// Generated call client type.
    /// This type inherits from the specified type and implements <see cref="ICallClient"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specified type is a value type.
    /// </exception>
    public static Type For(Type type)
    {
        // Call clients for value types are not supported
        // because the current implementation cannot determine whether the value instances is a proxy or not.
        return type.IsValueType
            ? throw new InvalidOperationException("Cannot generate a call client for a value type.")
            : Cache[type];
    }

    /// <summary>
    /// Generate the call client type for the specified type and instantiate an instance of it.
    /// </summary>
    /// <param name="type">Type for the call client to proxy.</param>
    /// <param name="serialization">Serializer providers.</param>
    /// <param name="transporter">Transporter for the call client to use.</param>
    /// <returns>Instantiated call client of the specified type.</returns>
    public static object New(Type type, IInjectionProvider serialization, ICallTransporter transporter)
    {
        var clientType = For(type);

        var missingTargets = InjectorOptions.PooledMissingTargets.Get();

        // Because the call client inherits from the target type,
        // it may contain required or injection members
        // that are not necessary for the call client itself.
        // Therefore, the injection is performed in 'best effort' mode,
        // and the missing targets are manually examined that whether they are serializers or not.
        var clientInstance = serialization.NewObject(clientType,
            InjectorOptions.Default with
            {
                SelectedMembers = SelectionMode.AttributedMembers,
                FailFast = false,
                MissingTargets = missingTargets
            });

        (clientInstance as ICallClient)!.CallTransporter = transporter;

        if (missingTargets.Count > 0)
        {
            var generatorName = typeof(CallClientGenerator).FullName;
            foreach (var target in missingTargets)
            {
                if (target.Member is FieldInfo field &&
                    SerializerDependencyAttribute.IsMarked(field, generatorName))
                    throw new InjectionFailureException(field.FieldType, serialization, target);
            }
        }

        InjectorOptions.PooledMissingTargets.Return(missingTargets);
        return clientInstance;
    }

    /// <summary>
    /// Generate the call client type for the specified type and instantiate an instance of it.
    /// </summary>
    /// <typeparam name="TObject">Type for the call client to proxy.</typeparam>
    /// <param name="serialization">Serializer providers.</param>
    /// <param name="transporter">Transporter for the call client to use.</param>
    /// <returns>Instantiated call client of the specified type.</returns>
    public static TObject New<TObject>(IInjectionProvider serialization, ICallTransporter transporter)
        where TObject : class
        => (TObject)New(typeof(TObject), serialization, transporter);

    private static Type GenerateClient(DynamicAssembly assembly, Type targetType)
    {
        var type = assembly.DefineClass(
            targetType.GetFriendlyTypeNameForDynamic("CallClient_"),
            parent: targetType);

        var context = new EmittingContext
        {
            TargetType = targetType,
            TypeContext = type,
            TransporterField = type.FieldFactory.DefineInstance("Transporter", typeof(ICallTransporter))
        };
        context.TransporterField.MarkAttribute(AttributeInjectionMember);

        context.TypeContext.ImplementInterface(typeof(ICallClient));

        // Implement the transporter property.
        var transporterGetter =
            context.TypeContext.MethodFactory.Instance.OverrideFunctor<ICallTransporter>(
                typeof(ICallClient).GetProperty(nameof(ICallClient.CallTransporter))!.GetMethod!);
        transporterGetter.Return(context.TransporterField.SymbolOf<ICallTransporter>(transporterGetter));
        var transporterSetter =
            context.TypeContext.MethodFactory.Instance.OverrideAction(
                typeof(ICallClient).GetProperty(nameof(ICallClient.CallTransporter))!.SetMethod!);
        context.TransporterField
            .SymbolOf(transporterSetter, transporterSetter.This())
            .AssignContent(transporterSetter.Argument(0, typeof(ICallTransporter)));
        transporterSetter.Return();

        var handlers = targetType
            .GetMethods()
            .Select(method => (method, ImplementProxyMethod(context, method)))
            .ToArray();

        // Build the type.
        context.TypeContext.Build();

        // Inject redirectors to original methods.
        foreach (var (original, proxy) in handlers)
        {
            // If the method is overriden, skip it.
            if (original.IsVirtual || original.IsAbstract)
                continue;
            // If the method is not implemented in IL, skip it.
            var flags = original.GetMethodImplementationFlags();
            if (!flags.HasFlag(MethodImplAttributes.IL))
                continue;
            // If the method is provided by the runtime, skip it.
            if (flags.HasFlag(MethodImplAttributes.InternalCall) ||
                flags.HasFlag(MethodImplAttributes.Native) ||
                flags.HasFlag(MethodImplAttributes.Unmanaged) ||
                flags.HasFlag(MethodImplAttributes.Runtime))
                continue;
            CurrentProxyMethod.Value = proxy.BuildingMethod;
            Patcher.Patch(original, transpiler: new HarmonyMethod(InjectProxiedMethod));
        }

        CurrentProxyMethod.Value = null!;

        return context.TypeContext.BuildingType;
    }

    private static DynamicMethod ImplementProxyMethod(
        in EmittingContext context, MethodInfo targetMethod)
    {
        var parameterDefinitions = targetMethod.GetParameters()
            .Select(parameter => new ParameterDefinition(parameter.ParameterType))
            .ToArray();
        var hasReturnValue = targetMethod.ReturnType != typeof(void);
        DynamicMethod method = hasReturnValue
            ? context.TypeContext.MethodFactory.Instance.DefineFunctor($"ProxyCall_{targetMethod.Name}",
                targetMethod.ReturnType, parameterDefinitions, methodModifier: MethodModifier.Virtual)
            : context.TypeContext.MethodFactory.Instance.DefineAction($"ProxyCall_{targetMethod.Name}",
                parameterDefinitions, methodModifier: MethodModifier.Virtual);

        if (targetMethod.IsVirtual || targetMethod.IsAbstract)
        {
            context.TypeContext.Builder.DefineMethodOverride(method.Builder, targetMethod);
        }

        var symbolThis = method.This<ICallClient>();
        var arguments = targetMethod.GetParameters()
            .Select((parameter, index) =>
                (Name: parameter.Name ?? index.ToString(), Symbol: method.Argument(index, parameter.ParameterType)))
            .ToArray();

        var fieldTransporter = context.TransporterField.SymbolOf<ICallTransporter>(method, symbolThis);

        // If the call transporter is null, throw an exception.
        using (method.If(fieldTransporter.IsNull()))
        {
            method.ThrowException<NullReferenceException>(
                "This call client is not bound to a call transporter.");
        }

        // Serialize arguments.
        var symbolSerializedArguments = method.New<ObjectValue>();
        foreach (var argument in arguments)
        {
            var symbolArgumentNode = symbolSerializedArguments.Invoke(
                target => target.CreateNode(Any<string>.Value),
                [method.Value(argument.Name)]);
            var argumentBasicType = argument.Symbol.BasicType;
            var symbolSerializer = context.GetSerializerField(argumentBasicType)
                .SymbolOf(method, symbolThis);
            symbolSerializer.Invoke(
                typeof(SnapshotSerializer<>).MakeGenericType(argumentBasicType)
                    .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                        [argumentBasicType.MakeByRefType(), typeof(SnapshotNode)])!,
                [argument.Symbol, symbolArgumentNode]);
        }

        // Perform the call.

        var variableSerializedResult = fieldTransporter
            .Invoke(target => target.Call(Any<int>.Value, Any<ObjectValue>.Value),
                [method.Value(targetMethod.MetadataToken), symbolSerializedArguments])
            .ToSymbol();

        if (!hasReturnValue)
            return method;

        // Deserialize result.
        var resultType = targetMethod.ReturnType;
        var fieldResultSerializer = context.GetSerializerField(resultType).SymbolOf(method, symbolThis);
        if (resultType.IsGenericType)
        {
            var resultGenericDefinition = resultType.GetGenericTypeDefinition();
            if (resultGenericDefinition == typeof(ValueTask<>))
            {
                var symbolTask = method.Invoke(
                    typeof(CallClientGenerator).GetMethod(nameof(DeserializeResultAsValueTask))!
                        .MakeGenericMethod(resultType.GetGenericArguments()[0]),
                    [fieldResultSerializer, variableSerializedResult])!;
                (method as DynamicMethod<Action<ISymbol>>)!.Return(symbolTask);
                return method;
            }

            if (resultGenericDefinition == typeof(Task))
            {
                var symbolValueTask = method.Invoke(
                    typeof(CallClientGenerator).GetMethod(nameof(DeserializeResultAsTask))!
                        .MakeGenericMethod(resultType.GetGenericArguments()[0]),
                    [fieldResultSerializer, variableSerializedResult])!;
                (method as DynamicMethod<Action<ISymbol>>)!.Return(symbolValueTask);
                return method;
            }
        }

        var variableResult = method.Invoke(
            typeof(CallClientGenerator).GetMethod(nameof(DeserializeResult))!
                .MakeGenericMethod(resultType),
            [fieldResultSerializer, variableSerializedResult])!;
        (method as DynamicMethod<Action<ISymbol>>)!.Return(variableResult);
        return method;
    }

    private static IEnumerable<CodeInstruction> InjectProxiedMethod(
        IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
    {
        var labelProxy = generator.DefineLabel();

        // Jump to the proxy if the target is an ICallClient.
        yield return new CodeInstruction(OpCodes.Ldarg_0);
        yield return new CodeInstruction(OpCodes.Isinst, typeof(ICallClient));
        yield return new CodeInstruction(OpCodes.Brtrue, labelProxy);

        foreach (var instruction in instructions)
            yield return instruction;


        yield return new CodeInstruction(OpCodes.Nop)
        {
            labels = { labelProxy }
        };
        // Load 'this' and arguments.
        yield return new CodeInstruction(OpCodes.Ldarg_0);
        for (var parameterIndex = 1; parameterIndex <= original.GetParameters().Length; parameterIndex++)
            yield return new CodeInstruction(OpCodes.Ldarg, parameterIndex);
        // Call the proxy method.
        yield return new CodeInstruction(OpCodes.Call, CurrentProxyMethod.Value);
        yield return new CodeInstruction(OpCodes.Ret);
    }

    private readonly struct EmittingContext()
    {
        public required Type TargetType { get; init; }

        public required DynamicType TypeContext { get; init; }

        public required DynamicField TransporterField { get; init; }

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