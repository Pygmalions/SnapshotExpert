﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SnapshotExpert.Utilities;

internal static partial class TypeExtensions
{
    /// <summary>
    /// Check if this type matches the target type: <br/>
    /// - This type is equal to the target type. <br/>
    /// - One type is a generic definition, and the other type has the same generic definition. <br/>
    /// - Both types are generic type parameter, regardless of their positions. <br/>
    /// - Both types are generic types with parameters, and the parameters appear in the same positions. <br/>
    /// - (When 'strict' is false) target type is a generic type parameters. <br/>
    /// </summary>
    /// <param name="self">This type to check.</param>
    /// <param name="target">Target type to match.</param>
    /// <param name="strict">
    /// If false, any type would match a generic type parameter.
    /// Default is false.
    /// </param>
    /// <returns>True if this type matches the specified target type, otherwise false.</returns>
    public static bool Matches(this Type self, Type target, bool strict = true)
    {
        if (!target.IsGenericType)
            return self == target;
        
        // Target is generic, while self is not. They are not equal.
        if (!self.IsGenericType)
            return false;
        
        if (target.IsGenericParameter)
            return !strict || self.IsGenericParameter;
        
        // Target is not a generic parameter, while self is.
        if (self.IsGenericParameter)
            return !strict;
        
        if (self.IsGenericTypeDefinition)
            return self == target.GetGenericTypeDefinition();
        if (target.IsGenericTypeDefinition)
            return target == self.GetGenericTypeDefinition();
        if (self.GetGenericTypeDefinition() != target.GetGenericTypeDefinition())
            return false;
        var selfArguments = self.GetGenericArguments();
        var otherArguments = target.GetGenericArguments();
        return selfArguments.Index()
            .All(argument => Matches(argument.Item, otherArguments[argument.Index]));
    }
    
    /// <summary>
    /// Erase the generic arguments in the deepest nest. <br/>
    /// For example, this method will convert Nullable{ValueTuple{int, string}} to Nullable{ValueTuple{T1, T2}};
    /// and then convert Nullable{ValueTuple{T1, T2}} to Nullable{T1}.
    /// Partially erasing is not supported,
    /// means that this method will erase all generic arguments at the same time,
    /// therefore Nullable{ValueTuple{int, ValueTuple{int, long}}} will be converted to Nullable{ValueTuple{T1, T2}}.
    /// </summary>
    /// <param name="type">Type to erase.</param>
    /// <returns>Type whose generic arguments of the deepest nest is erased.</returns>
    /// <exception cref="ArgumentException">
    /// Throw if the specified type is not a generic type.
    /// </exception>
    public static Type EraseDeepestGenericArguments(this Type type)
    {
        var processedType = ProcessGenericType(type);
        if (processedType == null)
            throw new ArgumentException($"Type '{type}' is not a generic type.", nameof(type));
        return processedType;

        Type? ProcessGenericType(Type target)
        {
            if (!target.IsGenericType)
                return null;
            if (target.IsGenericTypeDefinition)
                return null;
            var arguments = target.GetGenericArguments();
            if (arguments.Length == 1 && ProcessGenericType(arguments[0]) is {} argument)
                return target.GetGenericTypeDefinition().MakeGenericType(argument);
            return target.GetGenericTypeDefinition();
        }
    }

    /// <summary>
    /// Check if the specified type is inherited from a generic definition.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <param name="definitionType">Generic definition to check.</param>
    /// <param name="matchedBaseType">
    /// The specified type itself or one of its base types which has the same generic definition
    /// with the specified one.
    /// </param>
    /// <returns>True if a base type with the specific generic definition</returns>
    public static bool TryMatchGenericBaseType(this Type type, Type definitionType,
        [NotNullWhen(true)] out Type? matchedBaseType)
    {
        for (var current = type; current != null; current = type.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == definitionType)
            {
                matchedBaseType = current;
                return true;
            }
        }

        matchedBaseType = null;
        return false;
    }

    /// <summary>
    /// Check if the specific type has implemented the specified interface type.
    /// </summary>
    /// <param name="type">Type to check</param>
    /// <param name="interfaceType">Interface type to check, can be generic type definition.</param>
    /// <param name="matchedInterfaceType">
    /// The matched interface type, or null if not found.
    /// </param>
    /// <returns>True if the corresponding interface type is found, otherwise false.</returns>
    public static bool TryMatchInterface(this Type type, Type interfaceType,
        [NotNullWhen(true)] out Type? matchedInterfaceType)
    {
        // If the specified interface type is generic and all parameters are generic parameters,
        // then it will be treated as a generic definition.
        var isInterfaceGenericDefinition =
            interfaceType.IsGenericTypeDefinition;

        if (type == interfaceType || isInterfaceGenericDefinition && type.IsGenericType &&
            type.GetGenericTypeDefinition() == interfaceType)
        {
            matchedInterfaceType = type;
            return true;
        }

        // Here, matched interface type is probably an interface with the same name of the specified generic interface
        // and same number of generic arguments.
        matchedInterfaceType = type.GetInterface(interfaceType.Name);
        if (matchedInterfaceType == null)
            return false;
        if (matchedInterfaceType.Matches(interfaceType))
            return true;

        // Now the matchInterfaceType is matching a generic interface which has the same name and arguments number
        // as that of the specified generic interface.
        foreach (var candidateInterface in type.GetInterfaces())
        {
            if (isInterfaceGenericDefinition)
            {
                if (!candidateInterface.IsGenericType)
                    continue;
                if (candidateInterface.GetGenericTypeDefinition() != interfaceType)
                    continue;
                matchedInterfaceType = candidateInterface;
                return true;
            }

            if (candidateInterface != interfaceType)
                continue;
            matchedInterfaceType = candidateInterface;
            return true;
        }

        matchedInterfaceType = null;
        return false;
    }

    /// <summary>
    /// Get the backing field of the specified property.
    /// </summary>
    /// <param name="property">Property to get the backing field.</param>
    /// <returns>Backing field, or null if not found.</returns>
    public static FieldInfo? GetBackingField(this PropertyInfo property)
    {
        var flags = BindingFlags.NonPublic;
        if ((property.GetMethod ?? property.SetMethod)?.IsStatic == true)
            flags |= BindingFlags.Static;
        else
            flags |= BindingFlags.Instance;
        var fieldName = $"<{property.Name}>k__BackingField";
        return property.DeclaringType?.GetField(fieldName, flags);
    }

    /// <summary>
    /// Get the associated property of the specified backing field.
    /// </summary>
    /// <param name="field">Field to get the fronting property.</param>
    /// <returns>Property of this backing field, or null if not found.</returns>
    public static PropertyInfo? GetFrontingProperty(this FieldInfo field)
    {
        var match = BackingFieldNameMatcher().Match(field.Name);
        if (!match.Success || match.Groups.Count < 1)
            return null;
        var propertyName = match.Groups[1].Value;
        var flags = BindingFlags.NonPublic | BindingFlags.Public;
        if (field.IsStatic)
            flags |= BindingFlags.Static;
        else
            flags |= BindingFlags.Instance;
        return field.DeclaringType?.GetProperty(propertyName, flags);
    }

    [GeneratedRegex("<([^>]+)>k__BackingField")]
    private static partial Regex BackingFieldNameMatcher();

    /// <summary>
    /// Get the member type of the specified member: <br/>
    /// - PropertyInfo: property type
    /// - FieldInfo: field type
    /// - MethodInfo: return type
    /// - EventInfo: event handler type
    /// - ConstructorInfo: declaring type
    /// - Other: null.
    /// </summary>
    /// <param name="member">Member to get the type.</param>
    /// <returns>Member type.</returns>
    public static Type? GetMemberType(this MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            MethodInfo method => method.ReturnType,
            EventInfo @event => @event.EventHandlerType,
            ConstructorInfo constructor => constructor.DeclaringType,
            _ => throw new ArgumentException(
                $"Unsupported member type {member.MemberType}.", nameof(member))
        };
    }
}