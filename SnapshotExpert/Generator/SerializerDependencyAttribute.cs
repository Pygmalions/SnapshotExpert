using System.Reflection;
using System.Reflection.Emit;

namespace SnapshotExpert.Generator;

/// <summary>
/// This attribute marks a field a dependency serializer.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SerializerDependencyAttribute(string generator) : Attribute
{
    /// <summary>
    /// Name of the generator that produces this dependency.
    /// </summary>
    public string GeneratorName { get; } = generator;

    /// <summary>
    /// Create a custom attribute builder for this attribute with the specified name.
    /// </summary>
    /// <param name="name">Generate name to mark in the attribute.</param>
    /// <returns>Builder of this attribute with the specified name.</returns>
    public static CustomAttributeBuilder CreateBuilder(string name)
        => new(
            typeof(SerializerDependencyAttribute).GetConstructor([typeof(string)])!,
            [name]);

    /// <summary>
    /// Check if the specified member is marked with this attribute.
    /// </summary>
    /// <param name="member">Member to check.</param>
    /// <param name="name">Optional name to match.</param>
    /// <returns>
    /// True if the member is marked with this attribute and the specified name matches, otherwise false.
    /// </returns>
    public static bool IsMarked(MemberInfo member, string? name = null)
    {
        if (member.GetCustomAttribute<SerializerDependencyAttribute>() is not { } attribute)
            return false;
        return name is null || attribute.GeneratorName == name;
    }
}