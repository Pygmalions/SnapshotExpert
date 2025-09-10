using System.Reflection;
using System.Reflection.Emit;
using EmitToolbox.Extensions;
using EmitToolbox.Framework;

namespace SnapshotExpert.Generator;

public partial class SerializerGenerator
{
    private class ClassContext
    {
        private readonly Dictionary<Type, InstanceDynamicField> _serializers = new();

        public required Type TargetType { get; init; }
        
        public required DynamicType TypeContext { get; init; }
        
        public required Type SerializerBaseType { get; init; }

        public void EmitLoadTarget(ILGenerator code)
        {
            code.LoadArgument_1();
            if (!TargetType.IsValueType)
                code.Emit(OpCodes.Ldind_Ref);
        }
        
        public void EmitLoadSerializer(ILGenerator code, Type type)
        {
            code.LoadArgument_0();
            code.LoadField(GetSerializerField(type).BuildingField);
        }
        
        private InstanceDynamicField GetSerializerField(Type type)
        {
            if (_serializers.TryGetValue(type, out var field))
                return field;
            field = TypeContext.FieldBuilder.DefineInstance(
                "Serializer_" + type.ToString().Replace('.', '_'),
                typeof(SnapshotSerializer<>).MakeGenericType(type));
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