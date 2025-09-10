using System.Runtime.CompilerServices;

namespace SnapshotExpert.Generator.ElementDescriptors;

public class ElementFunctorDescriptor<TTarget, TMember> :
    ElementDescriptor where TTarget : notnull
{
    public delegate TMember GetterDelegate(in TTarget target);

    public delegate void SetterDelegate(ref TTarget target, in TMember value);

    private Func<ElementFunctorDescriptor<TTarget, TMember>, object, object?>? _generalGetter;

    private Action<ElementFunctorDescriptor<TTarget, TMember>, object, object?>? _generalSetter;

    public override Type TargetType { get; } = typeof(TTarget);

    public override Type MemberType { get; } = typeof(TMember);

    /// <summary>
    /// Functor to get the value of this member from the target.
    /// </summary>
    public required GetterDelegate Getter { get; init; }

    /// <summary>
    /// Functor to set the value of this member in the target.
    /// </summary>
    public required SetterDelegate Setter { get; init; }

    public override object? GetValue(object target)
    {
        _generalGetter ??= typeof(MemberAccessors)
            .GetMethod(typeof(TTarget).IsValueType
                ? nameof(MemberAccessors.StructMemberGetValue)
                : nameof(MemberAccessors.ClassMemberGetValue))!
            .MakeGenericMethod(typeof(TTarget), typeof(TMember))
            .CreateDelegate<Func<ElementFunctorDescriptor<TTarget, TMember>, object, object?>>();
        return _generalGetter(this, target);
    }

    public override void SetValue(object target, object? value)
    {
        _generalSetter ??= typeof(MemberAccessors)
            .GetMethod(typeof(TTarget).IsValueType
                ? nameof(MemberAccessors.StructMemberSetValue)
                : nameof(MemberAccessors.ClassMemberSetValue))!
            .MakeGenericMethod(typeof(TTarget), typeof(TMember))
            .CreateDelegate<Action<ElementFunctorDescriptor<TTarget, TMember>, object, object?>>();
        _generalSetter(this, target, value);
    }
}

internal static class MemberAccessors
{
    public static object? StructMemberGetValue<TTarget, TMember>(
        ElementFunctorDescriptor<TTarget, TMember> descriptor, object target)
        where TTarget : struct
    {
        if (target.GetType() != typeof(TTarget))
            throw new ArgumentException(
                "Specified target instance mismatch the target type of this descriptor.",
                nameof(target));
        return descriptor.Getter(Unsafe.Unbox<TTarget>(target));
    }

    public static void StructMemberSetValue<TTarget, TMember>(
        ElementFunctorDescriptor<TTarget, TMember> descriptor, object target, object? value)
        where TTarget : struct
    {
        if (target.GetType() != typeof(TTarget))
            throw new ArgumentException(
                "Specified target instance mismatch the target type of this descriptor.",
                nameof(target));
        if (value is not TMember typedValue)
            throw new ArgumentException(
                "Specified value instance mismatch the member type of this descriptor.",
                nameof(value));
        descriptor.Setter(ref Unsafe.Unbox<TTarget>(target), typedValue);
    }

    public static object? ClassMemberGetValue<TTarget, TMember>(
        ElementFunctorDescriptor<TTarget, TMember> descriptor, object target)
        where TTarget : class
    {
        if (target is not TTarget typedTarget)
            throw new ArgumentException(
                "Specified target instance mismatch the target type of this descriptor.",
                nameof(target));
        return descriptor.Getter(typedTarget);
    }

    public static void ClassMemberSetValue<TTarget, TMember>(
        ElementFunctorDescriptor<TTarget, TMember> descriptor, object target, object? value)
        where TTarget : class
    {
        if (target is not TTarget typedTarget)
            throw new ArgumentException(
                "Specified target instance mismatch the target type of this descriptor.",
                nameof(target));
        if (value is not TMember typedValue)
            throw new ArgumentException(
                "Specified value instance mismatch the member type of this descriptor.",
                nameof(value));
        descriptor.Setter(ref typedTarget, typedValue);
    }
}