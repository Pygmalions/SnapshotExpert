using System.Reflection;
using EmitToolbox;
using EmitToolbox.Extensions;
using EmitToolbox.Symbols;
using EmitToolbox.Utilities;
using SnapshotExpert.Data;
using SnapshotExpert.Data.Values;
using SnapshotExpert.Serializers;

namespace SnapshotExpert.Generator;

public partial class SerializerGenerator
{
    private struct SaverMethodBuilder() : ISerializerMethodBuilder
    {
        private ClassContext _context = null!;

        private DynamicMethod<Action> _method = null!;

        private ArgumentSymbol _argumentTarget = null!;

        private ArgumentSymbol<SnapshotNode> _argumentNode = null!;

        private ArgumentSymbol<SnapshotWritingScope> _argumentScope = null!;

        private VariableSymbol<ObjectValue> _variableObjectValue = null!;

        public void Initialize(ClassContext context)
        {
            _context = context;
            _method = context.TypeContext.MethodFactory.Instance
                .OverrideAction(context.SerializerBaseType.GetMethod(
                    !context.TargetType.IsValueType
                        ? "OnSaveSnapshot"
                        : nameof(SnapshotSerializerValueTypeBase<>.SaveSnapshot),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    [
                        context.TargetType.MakeByRefType(),
                        typeof(SnapshotNode),
                        typeof(SnapshotWritingScope)
                    ])!);
            _argumentTarget = _method.Argument(0, context.TargetType.MakeByRefType());
            _argumentNode = _method.Argument<SnapshotNode>(1);
            _argumentScope = _method.Argument<SnapshotWritingScope>(2);

            _variableObjectValue = _method.New<ObjectValue>();
        }

        public void Complete()
        {
            _argumentNode.SetPropertyValue(
                typeof(SnapshotNode).GetProperty(nameof(SnapshotNode.Value))!,
                _variableObjectValue);
            _method.Return();
        }

        public void Generate(FieldInfo field, MemberInfo metadata)
        {
            var fieldSerializer = _context.GetSerializerField(field.FieldType)
                .SymbolOf(_method, _method.This());
            var fieldMember = _argumentTarget.Field(field);
            var variableMemberNode = _variableObjectValue
                .Invoke(target => target.CreateNode(Any<string>.Value),
                    [_method.Literal(metadata.Name)]);

            fieldSerializer.Invoke(typeof(SnapshotSerializer<>)
                    .MakeGenericType(field.FieldType)
                    .GetMethod(nameof(SnapshotSerializer<>.SaveSnapshot),
                    [
                        field.FieldType.MakeByRefType(),
                        typeof(SnapshotNode),
                        typeof(SnapshotWritingScope)
                    ])!,
                [
                    fieldMember,
                    variableMemberNode,
                    _argumentScope
                ]);
        }
    }
}