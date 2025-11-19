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

namespace SnapshotExpert.Remoting.Generators;

public static class CallServerGenerator
{
    private static readonly CustomAttributeBuilder AttributeRequiredMember =
        new(typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

    private static readonly CustomAttributeBuilder AttributeInjectionMember =
        new(typeof(InjectionAttribute).GetConstructor([typeof(bool)])!, [true]);
    
    private static readonly CustomAttributeBuilder AttributeSerializerDependency =
        SerializerDependencyAttribute.CreateBuilder(typeof(CallServerGenerator).FullName!);
    
    private static readonly DynamicResourceForType<Type> Cache = new(
        GenerateServer, "CallServers_");

    /// <summary>
    /// Generate a call server type for the specified type.
    /// </summary>
    /// <param name="type">Type to handle.</param>
    /// <returns>
    /// Generated call server type.
    /// This type implements <see cref="ICallServer"/>.
    /// </returns>
    public static Type For(Type type) => Cache[type];

    /// <summary>
    /// Generate the call server type for the specified type and instantiate an instance of it.
    /// </summary>
    /// <param name="type">Type for the call server to proxy.</param>
    /// <param name="serialization">Serializer providers.</param>
    /// <returns>Instantiated call server of the specified type.</returns>
    public static object New(Type type, IInjectionProvider serialization)
    {
        var serverType = For(type);
        var serverInstance = serialization.NewObject(serverType);
        return serverInstance;
    }
    
    /// <summary>
    /// Generate the call server type for the specified type and instantiate an instance of it.
    /// </summary>
    /// <typeparam name="TObject">Type for the call server to proxy.</typeparam>
    /// <param name="serialization">Serializer providers.</param>
    /// <returns>Instantiated call server of the specified type.</returns>
    public static TObject New<TObject>(IInjectionProvider serialization) where TObject : class
        => (TObject)New(typeof(TObject), serialization);
    
    private static Type GenerateServer(DynamicAssembly assembly, Type targetType)
    {
        var context = new EmittingContext
        {
            TargetType = targetType,
            TypeContext = assembly
                .DefineClass(targetType.GetFriendlyTypeNameForDynamic("CallServer_"))
        };

        context.TypeContext.ImplementInterface(typeof(ICallServer));

        var handlers = targetType
            .GetMethods()
            .Select(method => (method, ImplementHandlerMethod(context, method)));

        ImplementDispatcherMethod(context, handlers);

        context.TypeContext.Build();
        return context.TypeContext.BuildingType;
    }

    private static void ImplementDispatcherMethod(
        in EmittingContext context, IEnumerable<(MethodInfo Method, DynamicFunction Handler)> handlers)
    {
        var method = context.TypeContext.MethodFactory.Instance.OverrideFunctor<SnapshotValue?>(
            typeof(ICallServer).GetMethod(nameof(ICallServer.HandleCall))!);

        var argumentTarget = method.Argument<object>(0);
        var argumentMethod = method.Argument<int>(1);
        var argumentArguments = method.Argument<ObjectValue>(2);

        var variableResult = method.Variable<SnapshotValue>();

        var argumentThis = method.This<ICallServer>();

        var labelEnd = method.DefineLabel();

        foreach (var (targetMethod, handler) in handlers)
        {
            using (method.If(argumentMethod.IsEqualTo(targetMethod.MetadataToken)))
            {
                argumentThis
                    .Invoke<SnapshotValue>(handler, [argumentTarget, argumentArguments])
                    .ToSymbol(variableResult);
                labelEnd.Goto();
            }
        }

        labelEnd.Mark();

        method.Return(variableResult);
    }

    private static DynamicFunction ImplementHandlerMethod(
        in EmittingContext context, MethodInfo targetMethod)
    {
        var method = context.TypeContext.MethodFactory.Instance.DefineFunctor<SnapshotValue?>(
            $"Handle_{targetMethod.Name}",
            [typeof(object), typeof(ObjectValue)]);
        var code = method.Code;

        var argumentTarget = method.Argument<object>(0);
        var argumentSerializedArguments = method.Argument<ObjectValue>(1);

        var variableRawTarget = context.TargetType.IsValueType
            ? argumentTarget.Unbox(context.TargetType, true).ToSymbol()
            : argumentTarget.CastTo(context.TargetType).ToSymbol();

        var variableArgumentNodes = argumentSerializedArguments
            .GetPropertyValue(target => target.Nodes)
            .ToSymbol();

        var variablesRawArgument = targetMethod.GetParameters()
            .Select(parameter => method.Variable(parameter.ParameterType))
            .ToArray();

        // Deserialize arguments.
        foreach (var (index, parameter) in targetMethod.GetParameters().Index())
        {
            var variableArgumentNode = method.Invoke(
                () => Any<IReadOnlyDictionary<string, SnapshotNode>>.Value.GetValueOrDefault(Any<string>.Value),
                [variableArgumentNodes, method.Value(parameter.Name ?? index.ToString())]);

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

        variableRawTarget.LoadAsTarget();
        foreach (var variableDeserializedArgument in variablesRawArgument)
            variableDeserializedArgument.LoadContent();
        code.Emit(targetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetMethod);

        if (!hasReturnValue)
        {
            method.Return(method.Null<SnapshotValue>());
        }
        else
        {
            var variableRawResult = method.Variable(targetMethod.ReturnType);
            variableRawResult.StoreContent();

            var variableResultNode = method.New(
                () => new SnapshotNode(Any<string>.Value), [method.Null<string>()]);

            // Serialize result.
            var fieldResultSerializer = context.GetSerializerField(targetMethod.ReturnType)
                .SymbolOf(method, method.This());
            fieldResultSerializer.Invoke(
                typeof(SnapshotSerializer<>).MakeGenericType(targetMethod.ReturnType)
                    .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                        [targetMethod.ReturnType.MakeByRefType(), typeof(SnapshotNode)])!,
                [variableRawResult, variableResultNode]
            );
            method.Return(variableResultNode.GetPropertyValue(target => target.Value));
        }

        return method;
    }

    private readonly struct EmittingContext()
    {
        public required DynamicType TypeContext { get; init; }

        public required Type TargetType { get; init; }

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