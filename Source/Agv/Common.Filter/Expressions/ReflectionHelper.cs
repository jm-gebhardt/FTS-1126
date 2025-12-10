using System.Reflection;
using Gebhardt.Acc.Common.Filter.Exceptions;

namespace Gebhardt.Acc.Common.Filter.Expressions;

/// <summary>
/// Contains helper methods around types.
/// </summary>
internal static class ReflectionHelper
{
    /// <summary>
    /// Extension block for <see cref="MemberInfo"/>.
    /// </summary>
    extension(MemberInfo member)
    {
        /// <summary>
        /// Checks if a member info is annotated as nullable. Unknown/Oblivious nullability state will be treated as nullable.
        /// </summary>
        public bool MemberMaybeNull()
        {
            // NullabilityInfoContext is not thread-safe, create a new instance each time
            // https://github.com/dotnet/runtime/issues/100254#issuecomment-2019353558
            var info = member.GetNullabilityInfo(new NullabilityInfoContext());

            return info?.ReadState != NullabilityState.NotNull;
        }

        /// <summary>
        /// Checks the nullability state of a member that is a collection type. Unknown/Oblivious nullability state will be treated as nullable.
        /// </summary>
        public bool CollectionMemberMaybeNull()
        {
            var info = member.GetNullabilityInfo(new NullabilityInfoContext());

            return info?.GenericTypeArguments.FirstOrDefault()?.ReadState != NullabilityState.NotNull;
        }

        private NullabilityInfo? GetNullabilityInfo(NullabilityInfoContext context)
        {
            return member switch
            {
                PropertyInfo property => context.Create(property),
                FieldInfo field => context.Create(field),
                EventInfo ev => context.Create(ev),
                _ => null
            };
        }

        /// <summary>
        /// Gets the type that this member accepts for an assignment.
        /// </summary>
        public Type? AcceptsType()
        {
            return member switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => null
            };
        }
    }

    /// <summary>
    /// Extension block for <see cref="Type"/>.
    /// </summary>
    extension(Type? type)
    {
        /// <summary>
        /// Get the content type of this <see cref="IEnumerable{T}"/> if it is one.
        /// </summary>
        public Type? GetEnumerableContentType()
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
        /// Gets a generic static method of this type with the provided parameters.
        /// </summary>
        /// <param name="name">The name of the method.</param>
        /// <param name="generics">The types of the type parameters.</param>
        /// <param name="paramCount">Optional number of parameters.</param>
        public MethodInfo GetGenericMethod(
            string name,
            Type[] generics,
            int? paramCount = null
        ) => type!
                 .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                 .Where(m => m.IsGenericMethodDefinition)
                 .First(m => m.Name == name && (paramCount == null || m.GetParameters().Length == paramCount))
                 .MakeGenericMethod(generics)
             ?? throw new FilterParserException($"Could not generate generic method {name}");
    }
}