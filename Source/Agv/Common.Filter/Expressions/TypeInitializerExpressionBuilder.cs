using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Exceptions;

namespace Gebhardt.Acc.Common.Filter.Expressions;

/// <summary>
/// Contains methods to create type initializer expressions.
/// </summary>
internal static class TypeInitializerExpressionBuilder
{
    private static MethodInfo GetGenericMethod(string name, Type[] generics, int? paramCount = null) => typeof(TypeInitializerExpressionBuilder).GetGenericMethod(name, generics, paramCount);

    /// <inheritdoc cref="CreateMemberInitializerExpression{TIn,TOut}"/>
    public static LambdaExpression CreateMemberInitializerExpression(Type domModel, Type readModel, IReadOnlyDictionary<Tuple<Type, Type>, LambdaExpression> subTypeBindings, Dictionary<string, string> translations, ICollection<string> directMembers)
    {
        if (!readModel.IsAssignableTo(domModel))
        {
            throw new FilterParserException($"Could not build member initializer for {domModel.Name} -> {readModel.Name} : Domain model not in read model hierarchy");
        }

        var projectionCreator = GetGenericMethod(nameof(CreateMemberInitializerExpression), [domModel, readModel], 3);
        return projectionCreator.Invoke(null, [subTypeBindings, translations, directMembers]) as LambdaExpression ?? throw new FilterParserException($"Could not build member initializer for {domModel.Name} -> {readModel.Name}");
    }

    /// <summary>
    /// Builds a member initializer for dynamically converting between two types.
    /// </summary>
    /// <typeparam name="TIn">Input type.</typeparam>
    /// <typeparam name="TOut">Output type.</typeparam>
    /// <param name="subTypeBindings">Individual subtypes that need to be handled specifically.</param>
    /// <param name="translations">Members where the names do not match.</param>
    /// <param name="directMembers">Members that should be directly transferred with no specific handling.</param>
    public static Expression<Func<TIn, TOut>> CreateMemberInitializerExpression<TIn, TOut>(IReadOnlyDictionary<Tuple<Type, Type>, LambdaExpression> subTypeBindings, Dictionary<string, string> translations, ICollection<string> directMembers) where TOut : class, TIn, new()
    {
        var param = Expression.Parameter(typeof(TIn), typeof(TIn).PrettyParameterName());

        var constructorInfo = typeof(TOut).GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder : null, types : Type.EmptyTypes, modifiers : null);
        // Valid use of null suppression, due to the new() constraint on TOut.
        var ctorExpression = Expression.New(constructorInfo!);

        MemberInfo[] members = typeof(TOut).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        List<MemberAssignment> memberBindings = [];

        foreach (var member in members)
        {
            if (translations.ContainsValue(member.Name))
            {
                continue;
            }

            if (member is not (FieldInfo or PropertyInfo))
            {
                continue;
            }

            if (member is PropertyInfo memberProperty && memberProperty.GetSetMethod(true) is null)
            {
                // TODO: add warning
                continue;
            }

            translations.TryGetValue(member.Name, out string? otherName);

            Expression valueExpression = Expression.PropertyOrField(param, otherName ?? member.Name);
            if (!directMembers.Contains(member.Name))
            {
                var sourceMember = typeof(TIn).GetMembers(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == (otherName ?? member.Name) && m is FieldInfo or PropertyInfo);

                var sourceMemberType = sourceMember switch
                {
                    FieldInfo f => f.FieldType,
                    PropertyInfo p => p.PropertyType,
                    _ => throw new FilterExecutionException($"Error creating Member initializer for {typeof(TIn).Name}, no matching member for {member.Name}")
                };

                var targetMemberType = member switch
                {
                    FieldInfo f => f.FieldType,
                    PropertyInfo p => p.PropertyType,
                    _ => throw new FilterExecutionException($"Error creating Member initializer for {typeof(TOut).Name}")
                };

                bool mightBeNull = sourceMember.MemberMaybeNull();

                if (targetMemberType.GetEnumerableContentType() is { } enumerableTargetContentType && sourceMemberType.GetEnumerableContentType() is { } enumerableSourceContentType)
                {
                    bool hasSubTypeBinding = subTypeBindings.TryGetValue(new Tuple<Type, Type>(enumerableSourceContentType, enumerableTargetContentType), out var enumerableSubTypeBinding);
                    bool collectionMemberMaybeNull = sourceMember.CollectionMemberMaybeNull();

                    valueExpression = CreateCollectionMemberInitializer(hasSubTypeBinding, enumerableSubTypeBinding, enumerableTargetContentType, collectionMemberMaybeNull, enumerableSourceContentType, valueExpression, mightBeNull, targetMemberType);
                }
                else if (subTypeBindings.TryGetValue(new Tuple<Type, Type>(sourceMemberType, targetMemberType), out var bindingExpression))
                {
                    if (mightBeNull)
                    {
                        bindingExpression = FilterExpressionBuilder.MakeNullCoalescing(bindingExpression.Parameters.First(), bindingExpression.Body, bindingExpression.ReturnType);
                    }
                    valueExpression = bindingExpression.Body.ReplaceParameter(bindingExpression.Parameters.First(),  valueExpression);
                }
            }
            
            memberBindings.Add(Expression.Bind(member, valueExpression));
        }
        
        var memberExpression = Expression.MemberInit(ctorExpression, memberBindings);

        return Expression.Lambda<Func<TIn, TOut>>(memberExpression, false, param);
    }

    private static Expression CreateCollectionMemberInitializer(bool hasSubTypeBinding, LambdaExpression? enumerableSubTypeBinding, Type enumerableTargetContentType, bool collectionMemberMaybeNull, Type enumerableSourceContentType, Expression valueExpression, bool mightBeNull, Type targetMemberType)
    {
        var resultType = hasSubTypeBinding ? enumerableSubTypeBinding!.ReturnType : enumerableTargetContentType;
        var selectProjection = hasSubTypeBinding ? enumerableSubTypeBinding! : ExpressionHelper.CreateIdentityExpression(resultType);
                    
        if (collectionMemberMaybeNull)
        {
            selectProjection = FilterExpressionBuilder.MakeNullCoalescing(selectProjection.Parameters.First(), selectProjection.Body, enumerableTargetContentType);
        }

        var selectMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m is { Name: nameof(Enumerable.Select), IsGenericMethodDefinition: true })
            .First(m => m.GetParameters() is [{ ParameterType.IsGenericType: true } p1, { ParameterType.IsGenericType: true } p2] && p1.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) && p2.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(enumerableSourceContentType, resultType);

        valueExpression = Expression.Call(null, selectMethod, valueExpression, selectProjection);

        if (mightBeNull)
        {
            valueExpression = FilterExpressionBuilder.MakeNullCoalescing(selectProjection.Parameters.First(), valueExpression, targetMemberType);
        }
        return valueExpression;
    }

    /// <summary>
    /// Creates a member initializer expression for re-creating an object with the old values and a mixin value.
    /// </summary>
    /// <typeparam name="T">Type to add the mixin into.</typeparam>
    /// <typeparam name="TInput">Type of the mixin.</typeparam>
    /// <param name="propertyName">The name of the mixin property on <typeparamref name="T"/>.</param>
    /// <param name="memberMapper">Expression to convert <typeparamref name="TInput"/> into the type of the property on <typeparamref name="T"/>.</param>
    public static Expression<Func<T, TInput, T>> CreateInitializerWithMixin<T, TInput>(string propertyName, LambdaExpression memberMapper) where T : class, new()
    {
        var baseParam = Expression.Parameter(typeof(T), typeof(T).PrettyParameterName());
        var memberParam = Expression.Parameter(typeof(TInput), typeof(TInput).PrettyParameterName());

        var memberMapperExpression = memberMapper.Body.ReplaceParameter(memberMapper.Parameters.First(), memberParam);

        var constructorInfo = typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null);
        // Valid use of null suppression, due to the new() constraint on T.
        var ctorExpression = Expression.New(constructorInfo!);

        MemberInfo[] members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        List<MemberAssignment> memberBindings = [];

        foreach (var member in members)
        {
            if (member is not (FieldInfo or PropertyInfo))
            {
                continue;
            }

            Expression valueExpression;
            try
            {
                valueExpression = member.Name == propertyName ? memberMapperExpression : Expression.PropertyOrField(baseParam, member.Name);
            }
            catch (AmbiguousMatchException)
            {
                if (member is not PropertyInfo propInfo)
                {
                    throw new FilterParserException($"Could not build combinator for {typeof(T).FullName} and {typeof(TInput).FullName}: Failed to create initializer for {member.Name}");
                }
                valueExpression = Expression.Property(baseParam, propInfo);
            }

            if (!valueExpression.Type.IsAssignableTo(member.AcceptsType()))
            {
                throw new FilterParserException($"Could not build combinator for {typeof(T).FullName} and {typeof(TInput).FullName}: Failed to create initializer for {member.Name}, Member type {member.AcceptsType()?.Name} does not accept {valueExpression.Type.Name}");
            }

            memberBindings.Add(Expression.Bind(member, valueExpression));
        }

        var memberExpression = Expression.MemberInit(ctorExpression, memberBindings);

        return Expression.Lambda<Func<T, TInput, T>>(memberExpression, false, baseParam, memberParam);
    }
}