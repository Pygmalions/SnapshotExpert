using System.Reflection;

namespace SnapshotExpert.Generator.ElementDescriptors;

public class ElementMemberDescriptor : ElementDescriptor
{
    public ElementMemberDescriptor(MemberInfo member)
    {
        Member = member;

        TargetType = member.DeclaringType!;
        switch (member)
        {
            case FieldInfo {IsInitOnly: false, IsLiteral: false} field:
                MemberType = field.FieldType;
                _generalGetter = instance => field.GetValue(instance);
                _generalSetter = (instance, value) => field.SetValue(instance, value);
                break;
            case PropertyInfo {CanRead: true, CanWrite: true} property:
                MemberType = property.PropertyType;
                _generalGetter = instance => property.GetValue(instance);
                _generalSetter = (instance, value) => property.SetValue(instance, value);
                break;
            default:
                throw new ArgumentException(
                    "Member must be a field or property that allows reading and writing.", nameof(member));
        }
    }
    
    /// <summary>
    /// Metadata for the member.
    /// </summary>
    public MemberInfo Member { get; }
    
    public override Type TargetType { get; }
    
    public override Type MemberType { get; }
    
    private readonly Func<object, object?> _generalGetter;

    private readonly Action<object, object?> _generalSetter;

    public override object? GetValue(object target) => _generalGetter(target);

    public override void SetValue(object target, object? value) => _generalSetter(target, value);
}