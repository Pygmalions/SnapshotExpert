using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using EmitToolbox;
using InjectionExpert;
using SnapshotExpert.Utilities;

namespace SnapshotExpert.Generator;

public partial class SerializerGenerator
{
    private static readonly CustomAttributeBuilder AttributeRequiredMember =
        new(typeof(RequiredMemberAttribute).GetConstructor(Type.EmptyTypes)!, []);

    private static readonly CustomAttributeBuilder AttributeInjectionMember =
        new(typeof(InjectionAttribute).GetConstructor([typeof(bool)])!, [true]);

    private static readonly CustomAttributeBuilder AttributeSerializerDependency =
        SerializerDependencyAttribute.CreateBuilder(typeof(SerializerGenerator).FullName!);
    
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
            field.MarkAttribute(AttributeInjectionMember);
            field.MarkAttribute(AttributeSerializerDependency);
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