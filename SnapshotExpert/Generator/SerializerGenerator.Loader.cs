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
    private struct LoaderMethodBuilder() : ISerializerMethodBuilder
    {
        private ClassContext _context = null!;

        private DynamicMethod<Action> _method = null!;

        /// <summary>
        /// Object value of the snapshot node.
        /// </summary>
        private VariableSymbol<ObjectValue> _variableObjectValue = null!;

        /// <summary>
        /// Sub node for members.
        /// </summary>
        private VariableSymbol<SnapshotNode> _variableMemberNode = null!;

        private ArgumentSymbol _argumentTarget = null!;

        private ArgumentSymbol<SnapshotReadingScope> _argumentScope = null!;

        public void Initialize(ClassContext context)
        {
            _context = context;
            _method = context.TypeContext.MethodFactory.Instance
                .OverrideAction(context.SerializerBaseType.GetMethod(
                    !context.TargetType.IsValueType
                        ? "OnLoadSnapshot"
                        : nameof(SnapshotSerializerValueTypeBase<>.LoadSnapshot),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    [
                        context.TargetType.MakeByRefType(),
                        typeof(SnapshotNode),
                        typeof(SnapshotReadingScope)
                    ])!);
            _argumentTarget =
                _method.Argument(0, context.TargetType.MakeByRefType());
            var argumentNode =
                _method.Argument<SnapshotNode>(1);
            _argumentScope =
                _method.Argument<SnapshotReadingScope>(2);

            _variableObjectValue = _method.Variable<ObjectValue>();
            _variableObjectValue.AssignContent(
                _method.Invoke(
                    () => Any<SnapshotNode>.Value.RequireValue<ObjectValue>(),
                    [argumentNode])
            );

            _variableMemberNode = _method.Variable<SnapshotNode>();
        }

        public void Complete()
        {
            _method.Return();
        }

        public void Generate(FieldInfo field, MemberInfo metadata)
        {
            var conditionFoundNode = _variableObjectValue
                .GetPropertyValue(target => target.Nodes)
                .Invoke<bool>(typeof(IReadOnlyDictionary<string, SnapshotNode>)
                        .GetMethod(nameof(IReadOnlyDictionary<,>.TryGetValue))!,
                    [_method.Value(metadata.Name), _variableMemberNode]);

            using (_method.If(conditionFoundNode))
            {
                var fieldSerializer = _context.GetSerializerField(field.FieldType)
                    .SymbolOf(_method, _method.This());
                var fieldMember = _argumentTarget.Field(field);

                fieldSerializer.Invoke(typeof(SnapshotSerializer<>)
                        .MakeGenericType(field.FieldType)
                        .GetMethod(nameof(SnapshotSerializer<>.LoadSnapshot),
                        [
                            field.FieldType.MakeByRefType(),
                            typeof(SnapshotNode),
                            typeof(SnapshotReadingScope)
                        ])!,
                    [
                        fieldMember,
                        _variableMemberNode,
                        _argumentScope
                    ]);
            }
        }
    }
}