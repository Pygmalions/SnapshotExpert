using System.Reflection;
using EmitToolbox.Framework;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Generator;

public partial class SerializerGenerator
{
    private class ClassContext
    {
        private readonly Dictionary<Type, DynamicField> _serializers = new();

        public required Type TargetType { get; init; }

        public required DynamicType TypeContext { get; init; }

        public required Type SerializerBaseType { get; init; }

        public DynamicField GetSerializerField(Type type)
        {
            if (_serializers.TryGetValue(type, out var field))
                return field;
            field = TypeContext.FieldFactory.DefineInstance(
                "Serializer_" + type.CreateDynamicFriendlyName(),
                typeof(SnapshotSerializer<>).MakeGenericType(type)
            );
            field.MarkAttribute(AttributeRequiredMember);
            _serializers[type] = field;
            return field;
        }
    }

    private interface ISerializerMethodBuilder
    {
        void Initialize(ClassContext context);

        void Complete();

        void Generate(FieldInfo field, MemberInfo metadata);
    }
}