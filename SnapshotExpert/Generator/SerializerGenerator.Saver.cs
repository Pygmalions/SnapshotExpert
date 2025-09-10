using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EmitToolbox.Extensions;
using EmitToolbox.Framework;
using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Generator;

public partial class SerializerGenerator
{
    private struct SaverMethodBuilder() : ISerializerMethodBuilder
    {
        private ClassContext _context = null!;

        private InstanceDynamicAction _method = null!;

        private LocalBuilder _variableObjectValue = null!;

        public void Initialize(ClassContext context)
        {
            _context = context;
            _method = context.TypeContext.ActionBuilder
                .Override(context.SerializerBaseType.GetMethod(
                    !context.TargetType.IsValueType
                        ? "OnSaveSnapshot"
                        : nameof(SnapshotSerializerValueTypeBase<>.SaveSnapshot),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    [
                        context.TargetType.MakeByRefType(),
                        typeof(SnapshotNode),
                        typeof(SnapshotWritingScope)
                    ])!);

            var code = _method.Code;
            _variableObjectValue = code.DeclareLocal(typeof(ObjectValue));
            code.NewObject(typeof(ObjectValue).GetConstructor(Type.EmptyTypes)!);
            code.StoreLocal(_variableObjectValue);
        }

        public void Complete()
        {
            var code = _method.Code;

            code.LoadArgument_2();
            code.LoadLocal(_variableObjectValue);
            code.StoreProperty(typeof(SnapshotNode).GetProperty(nameof(SnapshotNode.Value))!);

            code.MethodReturn();
        }

        public void Generate(FieldInfo field, MemberInfo metadata)
        {
            var code = _method.Code;

            _context.EmitLoadSerializer(code, field.FieldType);

            // Load target member address.
            _context.EmitLoadTarget(code);
            code.Emit(OpCodes.Ldflda, field);

            // Create sub-node.
            code.LoadLocal(_variableObjectValue);
            code.Emit(OpCodes.Ldstr, metadata.Name);
            code.Call(typeof(ObjectValue).GetMethod(nameof(ObjectValue.CreateNode),
                [typeof(string)])!);

            // Load snapshot writing scope.
            code.LoadArgument_3();

            code.CallVirtual(typeof(SnapshotSerializer<>)
                .MakeGenericType(field.FieldType)
                .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                [
                    field.FieldType.MakeByRefType(),
                    typeof(SnapshotNode),
                    typeof(SnapshotWritingScope)
                ])!);
        }
    }
}