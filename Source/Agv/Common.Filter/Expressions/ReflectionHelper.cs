using System.Reflection;
using Gebhardt.Acc.Common.Filter.Exceptions;

namespace Gebhardt.Acc.Common.Filter.Expressions;

/// <summary>
/// Contains helper methods around types.
/// </summary>
internal static class ReflectionHelper
{
    private static readonly NullabilityInfoContext _nullabilityContext = new();

    /// <summary>
    /// Checks if a member info is annotated as nullable. Unknown/Oblivious nullability state will be treated as nullable.
    /// </summary>
    public static bool MemberMaybeNull(this MemberInfo member)
    {
        var info = member.GetNullabilityInfo();

        return info?.ReadState != NullabilityState.NotNull;
    }

    /// <summary>
    /// Checks the nullability state of a member that is a collection type. Unknown/Oblivious nullability state will be treated as nullable.
    /// </summary>
    public static bool CollectionMemberMaybeNull(this MemberInfo collectionMember)
    {
        var info = collectionMember.GetNullabilityInfo();

        return info?.GenericTypeArguments.FirstOrDefault()?.ReadState != NullabilityState.NotNull;
    }

    private static NullabilityInfo? GetNullabilityInfo(this MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => _nullabilityContext.Create(property),
            FieldInfo field => _nullabilityContext.Create(field),
            EventInfo ev => _nullabilityContext.Create(ev),
            _ => null
        };
    }

    /// <summary>
    /// Get the content type of this <see cref="IEnumerable{T}"/> if it is one.
    /// </summary>
    public static Type? GetEnumerableContentType(this Type? type)
    {
        if (type == null)
        {
            return null;
        }

        if (type == typeof(string))
        {
            return null;
        }
        
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments().FirstOrDefault();
        }

        var enumerableType = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return  enumerableType?.GetGenericArguments().FirstOrDefault();
    }

    /// <summary>
    /// Gets the type that this member accepts for an assignment.
    /// </summary>
    public static Type? AcceptsType(this MemberInfo member)
    {
        return member switch
        {
            FieldInfo f => f.FieldType,
            PropertyInfo p => p.PropertyType,
            _ => null
        };
    }

    /// <summary>
    /// Gets a generic static method of this type with the provided parameters.
    /// </summary>
    /// <param name="type">The type to get the method from.</param>
    /// <param name="name">The name of the method.</param>
    /// <param name="generics">The types of the type parameters.</param>
    /// <param name="paramCount">Optional number of parameters.</param>
    public static MethodInfo GetGenericMethod(
        this Type type,
        string name,
        Type[] generics,
        int? paramCount = null
    ) => type
             .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
             .Where(m => m.IsGenericMethodDefinition)
             .First(m => m.Name == name && (paramCount == null || m.GetParameters().Length == paramCount))
             .MakeGenericMethod(generics)
         ?? throw new FilterParserException($"Could not generate generic method {name}");
}