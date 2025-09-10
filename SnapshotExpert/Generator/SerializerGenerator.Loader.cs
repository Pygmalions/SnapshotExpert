using System.Reflection;
using System.Reflection.Emit;
using EmitToolbox.Extensions;
using EmitToolbox.Framework;
using SnapshotExpert.Framework;
using SnapshotExpert.Framework.Values;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Generator;

public partial class SerializerGenerator
{
    private struct LoaderMethodBuilder() : ISerializerMethodBuilder
    {
        private ClassContext _context = null!;

        private InstanceDynamicAction _method = null!;

        /// <summary>
        /// Object value of the snapshot node.
        /// </summary>
        private LocalBuilder _variableObjectValue = null!;

        /// <summary>
        /// Sub node for members.
        /// </summary>
        private LocalBuilder _variableMemberNode = null!;

        public void Initialize(ClassContext context)
        {
            _context = context;
            _method = context.TypeContext.ActionBuilder
                .Override(context.SerializerBaseType.GetMethod(
                    !context.TargetType.IsValueType
                        ? "OnLoadSnapshot"
                        : nameof(SnapshotSerializerValueTypeBase<>.LoadSnapshot),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    [
                        context.TargetType.MakeByRefType(),
                        typeof(SnapshotNode),
                        typeof(SnapshotReadingScope)
                    ])!);

            _variableObjectValue = _method.Code.DeclareLocal(typeof(ObjectValue));
            _method.Code.LoadArgument_2();
            _method.Code.Call(
                typeof(SnapshotNodeExtensions)
                    .GetMethod(nameof(SnapshotNodeExtensions.RequireValue))!
                    .MakeGenericMethod(typeof(ObjectValue)));
            _method.Code.StoreLocal(_variableObjectValue);

            _variableMemberNode = _method.Code.DeclareLocal(typeof(SnapshotNode));
        }

        public void Complete()
        {
            _method.Code.MethodReturn();
        }

        public void Generate(FieldInfo field, MemberInfo metadata)
        {
            var code = _method.Code;

            // Locate the data entry for this member.
            // Currently, generated snapshot loader can tolerate missing data entries for members.
            var labelEnd = code.DefineLabel();
            EmitTryLocateMemberNode(metadata.Name);
            code.GotoIfFalse(labelEnd);

            _context.EmitLoadSerializer(code, field.FieldType);

            // Load target member address.
            _context.EmitLoadTarget(code);
            code.Emit(OpCodes.Ldflda, field);

            // Load snapshot node.
            code.LoadLocal(_variableMemberNode);

            // Load snapshot reading cope.
            code.LoadArgument_3();

            code.CallVirtual(typeof(SnapshotSerializer<>)
                .MakeGenericType(field.FieldType)
                .GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot),
                [
                    field.FieldType.MakeByRefType(),
                    typeof(SnapshotNode),
                    typeof(SnapshotReadingScope)
                ])!);

            code.MarkLabel(labelEnd);
        }

        private void EmitTryLocateMemberNode(string name)
        {
            var code = _method.Code;

            code.LoadNull();
            code.StoreLocal(_variableMemberNode);

            code.LoadLocal(_variableObjectValue);
            code.LoadProperty(typeof(ObjectValue).GetProperty(nameof(ObjectValue.Nodes))!);
            code.LoadLiteral(name);
            code.LoadLocalAddress(_variableMemberNode);
            code.Emit(OpCodes.Callvirt,
                typeof(IReadOnlyDictionary<string, SnapshotNode>)
                    .GetMethod(nameof(IReadOnlyDictionary<,>.TryGetValue))!);
        }

        private void EmitLocateMemberNode(string name)
        {
            var code = _method.Code;

            code.LoadLocal(_variableObjectValue);
            code.LoadLiteral(name);
            code.CallVirtual(
                typeof(ObjectValueExtensions).GetMethod(nameof(ObjectValueExtensions.RequireNode))!);
            code.StoreLocal(_variableMemberNode);
        }
    }
}