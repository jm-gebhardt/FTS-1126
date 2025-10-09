using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Gebhardt.Acc.Common.Filter.Expressions;

/// <summary>
/// Contains helper functions for generating filter expressions.
/// </summary>
[PublicAPI]
public static class FilterExpressionBuilder
{
    /// <summary>
    /// Gets a generic method of this class with the provided parameters.
    /// </summary>
    /// <param name="name">The name of the method.</param>
    /// <param name="generics">The types of the type parameters.</param>
    /// <param name="paramCount">Optional number of parameters.</param>
    private static MethodInfo GetGenericMethod(string name, Type[] generics, int? paramCount = null) => typeof(FilterExpressionBuilder)
        .GetMethods()
        .Where(m => m.IsGenericMethodDefinition)
        .First(m => m.Name == name && (paramCount == null || m.GetParameters().Length == paramCount))
        .MakeGenericMethod(generics)
        ?? throw new FilterParserException($"Could not generate generic method {name}");

    /// <summary>
    /// Creates a predicate that always evaluates to true.
    /// </summary>
    /// <param name="discardedInput">Type of the input parameter. Will not be used by the resulting lambda expression.</param>
    public static LambdaExpression CreateConstantTruePredicate(Type discardedInput) => Expression.Lambda(Expression.Constant(true, typeof(bool)), false, Expression.Parameter(discardedInput));

    /// <inheritdoc cref="CreatePropertyAccess{TInstance,TProperty}(string)"/>
    public static LambdaExpression CreatePropertyAccess(Type instanceType, Type propertyType, string propertyName)
    {
        return GetGenericMethod(nameof(CreatePropertyAccess), [instanceType, propertyType]).Invoke(null, [propertyName]) as LambdaExpression ?? throw new FilterParserException($"Could not create accessor for {propertyName} on {instanceType.Name}");
    }

    /// <summary>
    /// Creates an expression to access an arbitrary property on an object instance.
    /// </summary>
    /// <typeparam name="TInstance">Type of the object instance.</typeparam>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>An expression to access the property on the instance.</returns>
    public static Expression<Func<TInstance, TProperty>> CreatePropertyAccess<TInstance, TProperty>(string propertyName)
    {
        return CreatePropertyAccess<TInstance, TProperty>(propertyName, false);
    }

    private static Expression<Func<TInstance, TProperty>> CreatePropertyAccess<TInstance, TProperty>(string propertyName, bool shouldCast)
    {
        var instance = Expression.Parameter(typeof(TInstance), typeof(TInstance).PrettyParameterName());

        Expression property;
        try
        {
            property = Expression.PropertyOrField(instance, propertyName);
        } 
        catch (AmbiguousMatchException)
        {
            var propInfo = typeof(TInstance).GetProperty(propertyName, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            if (propInfo == null)
            {
                throw new FilterParserException($"Could not create accessor for {propertyName} on {typeof(TInstance).FullName}");
            }

            property = Expression.Property(instance, propInfo);
        }

        if (shouldCast)
        {
            property = Expression.Convert(property, typeof(TProperty));
        }

        return Expression.Lambda<Func<TInstance, TProperty>>(property, instance);
    }

    /// <inheritdoc cref="CreatePropertyAccessWithCast{TInstance,TProperty}"/>
    public static LambdaExpression CreatePropertyAccessWithCast(Type instanceType, Type propertyType, string propertyName)
    {
        return GetGenericMethod(nameof(CreatePropertyAccessWithCast), [instanceType, propertyType]).Invoke(null, [propertyName]) as LambdaExpression ?? throw new FilterParserException($"Could not create accessor for {propertyName} on {instanceType.Name}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TInstance"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static Expression<Func<TInstance, TProperty>> CreatePropertyAccessWithCast<TInstance, TProperty>(string propertyName)
    {
        return CreatePropertyAccess<TInstance, TProperty>(propertyName, true);
    }

    /// <summary>
    /// Creates an expression to compare a comparable type to a property of the same type on another object.
    /// </summary>
    /// <typeparam name="TProperty">The comparable object.</typeparam>
    /// <typeparam name="TInstance">The type that contains the comparable property.</typeparam>
    /// <param name="other">The object that contains the comparable property.</param>
    /// <param name="propertyName">The name of the comparable property.</param>
    /// <param name="accessor"></param>
    /// <returns>An expression that compares an input to the property on the other object.</returns>
    public static Expression<Func<TInstance, int>> CreateCompareToExpression<TInstance, TProperty>(TProperty other, string propertyName, Expression<Func<TInstance, TProperty>>? accessor = null)
    {
        var otherConstant = Expression.Constant(other, typeof(TProperty));

        var compareMethod = typeof(TProperty).GetMethod(nameof(IComparable.CompareTo), [typeof(TProperty)]);

        if (compareMethod == null)
        {
            throw new FilterBuilderException($"Could not fetch method information of CompareTo even though {typeof(TProperty).Name} implements IComparable.");
        }
        var otherParam = Expression.Parameter(typeof(TProperty), "other");

        var compareBody = Expression.Call(otherConstant, compareMethod, otherParam);

        bool noAccessorNeeded = typeof(TInstance) == typeof(TProperty) && propertyName == string.Empty;
        if (noAccessorNeeded)
        {
            return Expression.Lambda<Func<TInstance, int>>(compareBody, otherParam);
        }

        Expression<Func<TProperty, int>> compareCall = Expression.Lambda<Func<TProperty, int>>(compareBody, otherParam);
        Expression<Func<TInstance, TProperty>> propAccess = accessor ?? CreatePropertyAccess<TInstance, TProperty>(propertyName);

        return Compose(propAccess, compareCall);
    }

    /// <inheritdoc cref="ConnectPredicates{T}"/>
    public static LambdaExpression ConnectPredicates(LambdaExpression p1, LambdaExpression p2, LogicalCombinationVariant combinationVariantType)
    {
        if (p1.ReturnType != p2.ReturnType || p1.ReturnType != typeof(bool))
        {
            throw new FilterBuilderException($"Could not connect predicates, return types were not both bool: {p1.ReturnType}, {p2.ReturnType}");
        }

        var firstInputType = p1.Parameters.First().Type;

        var secondInputType = p2.Parameters.First().Type;

        if (firstInputType != secondInputType)
        {
            throw new FilterBuilderException($"Could not connect predicates, input types did not match: {firstInputType}, {secondInputType}");
        }

        var combinator = GetGenericMethod(nameof(ConnectPredicates), [firstInputType]);

        return combinator.Invoke(null, [p1, p2, combinationVariantType]) as LambdaExpression ?? throw new FilterBuilderException("Could not connect predicates; result not LambdaExpression");
    }

    /// <summary>
    /// Combines two predicate expressions into a new predicate expression with the logical operator provided.
    /// </summary>
    /// <typeparam name="T">Input type of the predicates.</typeparam>
    /// <param name="predicate1">First predicate.</param>
    /// <param name="predicate2">Second predicate.</param>
    /// <param name="op">The logical operation that combines the predicates.</param>
    public static Expression<Func<T, bool>> ConnectPredicates<T>(Expression<Func<T, bool>> predicate1, Expression<Func<T, bool>> predicate2, LogicalCombinationVariant op)
    {
        var param = Expression.Parameter(typeof(T), typeof(T).PrettyParameterName());

        var left = new ParameterReplacer(predicate1.Parameters.First(), param).Visit(predicate1.Body);
        var right = new ParameterReplacer(predicate2.Parameters.First(), param).Visit(predicate2.Body);

        var combination = op switch
        {
            LogicalCombinationVariant.And => Expression.AndAlso(left, right),
            LogicalCombinationVariant.Or => Expression.OrElse(left, right),
            _ => throw new FilterBuilderException($"Could not find logical operator for combining filters {op.ToString()}")
        };

        return Expression.Lambda<Func<T, bool>>(combination, param);
    }

    /// <summary>
    /// Creates an identity function of a specific type.
    /// </summary>
    /// <typeparam name="T">Type of the identity function.</typeparam>
    public static Expression<Func<T, T>> CreateIdentityExpression<T>()
    {
        var param = Expression.Parameter(typeof(T), typeof(T).PrettyParameterName());
        return Expression.Lambda<Func<T, T>>(param, param);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TInstance"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    /// <param name="collection"></param>
    /// <param name="parameterName"></param>
    /// <returns></returns>
    public static Expression<Func<TInstance, bool>> CreateIsOneOfPredicate<TInstance, TProperty>(ICollection<TProperty> collection, string parameterName)
    {
        Expression<Func<TInstance, TProperty>> accessor = CreatePropertyAccess<TInstance, TProperty>(parameterName);

        var param = Expression.Parameter(typeof(TProperty), typeof(TProperty).PrettyParameterName());

        var collectionConst = Expression.Constant(collection, typeof(ICollection<TProperty>));

        var methodInfo = typeof(ICollection<TProperty>).GetMethod(nameof(ICollection<TProperty>.Contains), [typeof(TProperty)]) ?? throw new FilterBuilderException($"Collection {collection} contains no definition of contains");

        var call = Expression.Call(collectionConst, methodInfo, param);

        Expression<Func<TProperty, bool>> lambda = Expression.Lambda<Func<TProperty, bool>>(call, param);

        return Compose(accessor, lambda);
    }

    /// <summary>
    /// Creates an expression to convert one type into another if possible. Works for reference and value types both.
    /// </summary>
    /// <param name="property">The access expression to convert.</param>
    /// <param name="outType">The type to convert to.</param>
    /// <returns>A unary expression converting to the type.</returns>
    private static UnaryExpression CreateConvertExpression(MemberExpression property, Type outType)
    {
        return outType.IsValueType ? Expression.Convert(property, outType) : Expression.TypeAs(property, outType);
    }

    /// <summary>
    /// Generates a predicate to for a comparison.
    /// </summary>
    /// <typeparam name="T">The input type of the predicate</typeparam>
    /// <param name="compareExpression">An expression containing a CompareTo call and a closure of the value to compare to.</param>
    /// <param name="variant">The variant of the comparison.</param>
    /// <returns>An expression containing the predicate comparing the incoming value with the closure according to the <see cref="ComparisonVariant"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the variant does not exist.</exception>
    private static Expression<Func<T, bool>> GeneratePredicate<T>(Expression<Func<T, int>> compareExpression, ComparisonVariant variant)
    {
        var param = Expression.Parameter(typeof(int), "i");
        var zero = Expression.Constant(0, typeof(int));

        var comparison = variant switch
        {
            ComparisonVariant.Gt => Expression.LessThan(param, zero),
            ComparisonVariant.Lt => Expression.GreaterThan(param, zero),
            ComparisonVariant.Leq => Expression.GreaterThanOrEqual(param, zero),
            ComparisonVariant.Geq => Expression.LessThanOrEqual(param, zero),
            ComparisonVariant.Equals => Expression.Equal(param, zero),
            ComparisonVariant.Neq => Expression.NotEqual(param, zero),
            _ => throw new FilterExecutionException($"Unknown operation type in this context: {variant.ToString()}")
        };

        Expression<Func<int, bool>> result = Expression.Lambda<Func<int, bool>>(comparison, param);

        return Compose(compareExpression, result);
    }

    /// <inheritdoc cref="GenerateContainsPredicate{T}"/>
    public static LambdaExpression GenerateContainsPredicate(Type inputType, object? other, string name, LambdaExpression? accessor)
    {
        if (other is not string)
        {
            throw new FilterBuilderException($"Could not construct contains-predicate expression for {name}, value is not a string.");
        }

        var generator = GetGenericMethod(nameof(GenerateContainsPredicate), [inputType]);

        return generator.Invoke(null, [other, name, accessor]) as LambdaExpression ?? throw new FilterBuilderException($"Could not construct contains-predicate expression for {name}");
    }

    /// <summary>
    /// Generates a predicate that tests if a string property on an input object contains the closure string.
    /// </summary>
    /// <typeparam name="T">The predicate input type.</typeparam>
    /// <param name="other">The closure string.</param>
    /// <param name="propertyName">The name of the string property on the closure string.</param>
    /// <param name="accessor">Optional accessor of the string property.</param>
    public static Expression<Func<T, bool>> GenerateContainsPredicate<T>(string other, string propertyName, Expression<Func<T, string>>? accessor = null) where T : class
    {
        Expression<Func<T, string>> propertyAccess = accessor ?? CreatePropertyAccess<T, string>(propertyName);

        var otherConstant = Expression.Constant(other, typeof(string));

        var containsMethod = typeof(string).GetMethod(nameof(other.Contains), [typeof(string)]);

        if (containsMethod == null)
        {
            throw new FilterBuilderException("String contains no definition for Contains.");
        }

        var param = Expression.Parameter(typeof(string), "input");

        var compareBody = Expression.Call(param, containsMethod, otherConstant);

        Expression<Func<string, bool>> compareLambda = Expression.Lambda<Func<string, bool>>(compareBody, param);

        return Compose(propertyAccess, compareLambda);
    }

    /// <inheritdoc cref="GeneratePredicate{T,TP}(TP,string,ComparisonVariant,Expression{Func{T, TP}}?)"/>
    public static LambdaExpression GeneratePredicate(Type inputType, Type otherType, object? other, string name, ComparisonVariant comparison, LambdaExpression? accessor, bool isNullable)
    {
        if (other?.GetType() != otherType)
        {
            throw new FilterBuilderException($"Could not create filter leaf expression for {name}, type of other does not match the supposed type ({other?.GetType()} != {otherType})");
        }
        
        var generator = GetGenericMethod(nameof(GeneratePredicate), [inputType, otherType], isNullable ? 3 : 4);

        object?[] parameters = isNullable ? [other, comparison, accessor] : [other, name, comparison, accessor];

        var predicate = generator.Invoke(null, parameters) as LambdaExpression ?? throw new InvalidCastException($"Could not construct filter leaf expression for {name}");
        return predicate;
    }

    /// <summary>
    /// Generates a predicate that compares a property on the input type to the provided closure according to the <see cref="ComparisonVariant"/>.
    /// </summary>
    /// <typeparam name="T">Input type of the predicate.</typeparam>
    /// <typeparam name="TProperty">Type of the property to compare.</typeparam>
    /// <param name="other">The closure value to compare with.</param>
    /// <param name="propertyName">The name of the property to compare.</param>
    /// <param name="variant">The variant of the comparison.</param>
    /// <param name="accessor">Optional accessor of the property.</param>
    public static Expression<Func<T, bool>> GeneratePredicate<T, TProperty>(TProperty other, string propertyName, ComparisonVariant variant, Expression<Func<T, TProperty>>? accessor = null)
    {
        bool isComparable = typeof(TProperty).IsAssignableTo(typeof(IComparable<TProperty>));
        bool canCompare = isComparable || variant is ComparisonVariant.Equals or ComparisonVariant.Neq;

        if (!canCompare)
        {
            throw new FilterExecutionException($"Cannot build comparison predicate for type {typeof(TProperty).FullName} and comparison {variant.ToString()}");
        }

        if (!isComparable)
        {
            Expression<Func<T, bool>> predicate = GenerateEqualsPredicate(other, propertyName, accessor);

            return variant is ComparisonVariant.Neq ? NegatePredicate(predicate) : predicate;
        }

        Expression<Func<T, int>> comparison = CreateCompareToExpression(other, propertyName, accessor);

        return GeneratePredicate(comparison, variant);
    }

    /// <inheritdoc cref="GeneratePredicate{T}"/>
    public static Expression<Func<T, bool>> GeneratePredicate<T, TProperty>(TProperty other, ComparisonVariant variant, Expression<Func<T, TProperty?>> accessor) where TProperty : struct
    {
        Expression<Func<TProperty?, bool>> nullcheck = CreateNotNullCheck<TProperty?>();

        Expression<Func<T, bool>> propertyNullcheck = Compose(accessor, nullcheck);

        var body = Expression.Convert(accessor.Body, typeof(TProperty));
        Expression<Func<T, TProperty>> notnullAccessor = Expression.Lambda<Func<T, TProperty>>(body, false, accessor.Parameters.First());

        Expression<Func<T, bool>> predicate = GeneratePredicate(other, "", variant, notnullAccessor);

        return ConnectPredicates(propertyNullcheck, predicate, LogicalCombinationVariant.And);
    }

    /// <summary>
    /// Creates an equality comparer.
    /// </summary>
    /// <typeparam name="T">Input of the predicate.</typeparam>
    /// <typeparam name="TProperty">Type of the property to compare.</typeparam>
    /// <param name="other">The closure value to compare to.</param>
    /// <param name="propertyName">The name of the property to compare to.</param>
    /// <param name="accessor">Optional accessor of the property.</param>
    public static Expression<Func<T, bool>> GenerateEqualsPredicate<T, TProperty>(TProperty other, string propertyName, Expression<Func<T, TProperty>>? accessor = null)
    {
        accessor ??= CreatePropertyAccess<T, TProperty>(propertyName);

        var param = Expression.Parameter(typeof(TProperty), typeof(TProperty).PrettyParameterName());
        
        var otherConst = Expression.Constant(other, typeof(TProperty));

        var equalsExpression = Expression.Equal(param, otherConst);

        Expression<Func<TProperty, bool>> localPredicate = Expression.Lambda<Func<TProperty, bool>>(equalsExpression, false, param);

        return Compose(accessor, localPredicate);
    }

    /// <summary>
    /// Creates a check to see if a value of the given type is null or not.
    /// </summary>
    /// <typeparam name="T">The type to generate the check for.</typeparam>
    /// <returns></returns>
    public static Expression<Func<T, bool>> CreateNotNullCheck<T>()
    {
        var param = Expression.Parameter(typeof(T), typeof(T).PrettyParameterName());

        var nullConstant = Expression.Constant(null, typeof(T?));
        
        return Expression.Lambda<Func<T, bool>>(
            Expression.NotEqual(param, nullConstant),
            false,
            param
        );
    }

    /// <summary>
    /// Negates a predicate.
    /// </summary>
    /// <typeparam name="T">Input type of the predicate.</typeparam>
    /// <param name="predicate">The predicate to invert.</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> NegatePredicate<T>(Expression<Func<T, bool>> predicate)
    {
        var param = predicate.Parameters.First();

        var body = Expression.Not(predicate.Body);

        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    /// <summary>
    /// Creates a predicate expression that redirects the provided predicate into an aggregate function on a collection.
    /// </summary>
    /// <typeparam name="TInput">The type of the elements in the collection.</typeparam>
    /// <param name="elementPredicate">The predicate to be applied to each element.</param>
    /// <param name="variant">The variant of the aggregation.</param>
    public static Expression<Func<IEnumerable<TInput>, bool>> CreateAggregationPredicate<TInput>(Expression<Func<TInput, bool>> elementPredicate, CollectionAggregateVariant variant)
    {
        string variantName = variant switch
        {
            CollectionAggregateVariant.All => nameof(Enumerable.All),
            CollectionAggregateVariant.Any => nameof(Enumerable.Any),
            _ => throw new FilterBuilderException($"Unsupported collection aggregate {variant.ToString()}")
        };

        var param = Expression.Parameter(typeof(IEnumerable<TInput>), "input");
        
        var expression = Expression.Call(typeof(Enumerable), variantName, [typeof(TInput)], param, elementPredicate);

        return Expression.Lambda<Func<IEnumerable<TInput>, bool>>(expression, param);
    }
    
    /// <inheritdoc cref="Compose{T,T,T,T}"/>
    public static LambdaExpression Compose(LambdaExpression first, LambdaExpression second)
    {
        var firstInputType = first.Parameters.First().Type;
        var secondInputType = second.Parameters.First().Type;

        if (!first.ReturnType.IsAssignableTo(secondInputType))
        {
            throw new FilterParserException($"Could not compose lambdas, type mismatch: out {first.ReturnType}, in {secondInputType}");
        }

        var concatMethod = GetGenericMethod(nameof(Compose), [firstInputType, first.ReturnType, secondInputType, second.ReturnType]);

        return concatMethod.Invoke(null, [first, second]) as LambdaExpression ?? throw new FilterParserException("Could not compose lambdas");
    }

    /// <summary>
    /// Composes two expressions where the return type of the first matches the parameter of the second so that they will be called in sequence.
    /// </summary>
    /// <typeparam name="TIn">Input parameter type.</typeparam>
    /// <typeparam name="TIntermediate">Overlapping parameter type.</typeparam>
    /// <typeparam name="TOut">Output type.</typeparam>
    /// <typeparam name="TIntermediateInterface">Least required type that the second function requires as an input.</typeparam>
    /// <param name="first">First function.</param>
    /// <param name="second">Second function.</param>
    /// <returns>A composition of both input functions.</returns>
    public static Expression<Func<TIn, TOut>> Compose<TIn, TIntermediate, TIntermediateInterface, TOut>(Expression<Func<TIn, TIntermediate>> first, Expression<Func<TIntermediateInterface, TOut>> second) where TIntermediate : TIntermediateInterface
    {
        var param = first.Parameters.First();

        var newBody = new ParameterReplacer(second.Parameters.First(), first.Body).Visit(second.Body);

        return Expression.Lambda<Func<TIn, TOut>>(newBody, param);
    }

    private class ParameterReplacer(ParameterExpression oldParam, Expression newExpr) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression current)
        {
            return current == oldParam ? newExpr : base.VisitParameter(current);
        }
    }

    /// <summary>
    /// Rebuilds the LambdaExpression as a typed predicate if it matches the types.
    /// Throws <see cref="FilterExecutionException"/> if not.
    /// </summary>
    public static Expression<Func<T, bool>> AsPredicate<T>(this LambdaExpression expression)
    {
        if (expression is Expression<Func<T, bool>> convertedExpression)
        {
            return convertedExpression;
        }

        if (expression.Parameters.Count != 1 || expression.Parameters.First().Type != typeof(T))
        {
            throw new FilterExecutionException($"Could not convert lambda to predicate, Parameters: {expression.Parameters.Count}, {expression.Parameters.FirstOrDefault()?.Type.FullName}");
        }

        if (!expression.ReturnType.IsAssignableTo(typeof(bool)))
        {
            throw new FilterExecutionException($"Could not convert lambda to predicate, Return: {expression.ReturnType.FullName}");
        }

        return Expression.Lambda<Func<T, bool>>(expression.Body, expression.TailCall, expression.Parameters);
    }
    
    private static TQr ApplyAccessor<TQr, TQi, T>(this TQi queryable, string method, Type type, LambdaExpression expression) where TQi : IQueryable<T>
    {
        if (expression.Parameters.Count != 1 || expression.Parameters.First().Type != typeof(T))
        {
            throw new FilterExecutionException($"Could not convert lambda to predicate, Parameters: {expression.Parameters.Count}, {expression.Parameters.FirstOrDefault()?.Type.FullName}");
        }

        var includeMethod = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == method && m.IsGenericMethodDefinition)
            .First(m =>
            {
                ParameterInfo[] ps = m.GetParameters();
                return ps is [{ ParameterType.IsGenericType: true }, { ParameterType.IsGenericType: true }]
                       && ps[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>)
                       && ps[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>);
            }).MakeGenericMethod(typeof(T), expression.ReturnType);

        if (includeMethod.Invoke(null, [queryable, expression]) is not TQr result)
        {
            throw new FilterExecutionException($"Could not apply {method} on queryable {typeof(T)}");
        }

        return result;
    }

    /// <summary>
    /// Creates a member initializer expression for re-creating an object with the old values and a mixin value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TMember"></typeparam>
    /// <param name="propertyName"></param>
    /// <param name="memberMapper"></param>
    /// <returns></returns>
    public static Expression<Func<T, TInput, T>> CreateInitializerWithMixin<T, TInput, TMember>(string propertyName, Expression<Func<TInput, TMember>> memberMapper) where T : class, new()
    {
        return CreateInitializerWithMixin<T, TInput, TMember>(propertyName, (LambdaExpression)memberMapper);
    }

    /// <inheritdoc cref="CreateInitializerWithMixin{T,TInput,TMember}(string,System.Linq.Expressions.Expression{System.Func{TInput,TMember}})"/>>
    public static Expression<Func<T, TInput, T>> CreateInitializerWithMixin<T, TInput, TMember>(string propertyName, LambdaExpression memberMapper) where T : class, new()
    {
        var baseParam = Expression.Parameter(typeof(T), typeof(T).PrettyParameterName());
        var memberParam = Expression.Parameter(typeof(TInput), typeof(TInput).PrettyParameterName());

        var memberMapperExpression = new ParameterReplacer(memberMapper.Parameters.First(), memberParam).Visit(memberMapper.Body);

        // Valid use of null suppression, due to the new() constraint on T.
        var constructorInfo = typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null);
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
            
            memberBindings.Add(Expression.Bind(member, valueExpression));
        }

        var memberExpression = Expression.MemberInit(ctorExpression, memberBindings);

        return Expression.Lambda<Func<T, TInput, T>>(memberExpression, false, baseParam, memberParam);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="elementType"></param>
    /// <param name="inputType"></param>
    /// <param name="elementPredicate"></param>
    /// <param name="accessor"></param>
    /// <param name="aggregateVariant"></param>
    public static LambdaExpression BuildAggregationPredicate(Type elementType, Type inputType, LambdaExpression elementPredicate, LambdaExpression accessor, CollectionAggregateVariant aggregateVariant)
    {
        var predicateInput = elementPredicate.Parameters.First().Type;

        if (elementPredicate.ReturnType != typeof(bool))
        {
            throw new FilterBuilderException($"Could not create aggregation predicate expression tree, predicate expression does not return bool but {elementPredicate.ReturnType}");
        }

        if (elementType != predicateInput)
        {
            throw new FilterBuilderException($"Could not create aggregation predicate expression tree, element type {elementType} does not match predicate input {predicateInput}");
        }

        var outputType = accessor.ReturnType;

        var generator = GetGenericMethod(nameof(CreateAggregationPredicate), [elementType]);

        var aggregation = generator.Invoke(null, [elementPredicate, aggregateVariant]) as LambdaExpression;

        var aggregationInputType = aggregation?.Parameters.FirstOrDefault()?.Type ?? outputType;

        if (!outputType.IsAssignableTo(aggregationInputType))
        {
            throw new FilterBuilderException($"Could not create aggregation predicate expression tree, accessor output {outputType} not assignable to aggregate input {aggregationInputType}");
        }

        var composer = GetGenericMethod(nameof(Compose), [inputType, outputType, aggregationInputType, typeof(bool)]);

        return composer.Invoke(null, [accessor, aggregation]) as LambdaExpression ?? throw new FilterBuilderException("Could not create aggregation predicate expression tree");
    }

    /// <inheritdoc cref="CreateMemberInitializerExpression{TIn,TOut}"/>
    public static LambdaExpression CreateMemberInitializerExpression(Type domModel, Type readModel, IReadOnlyDictionary<Tuple<Type, Type>, LambdaExpression> subTypeBindings, Dictionary<string, string> translations)
    {
        if (!readModel.IsAssignableTo(domModel))
        {
            throw new FilterParserException($"Could not build member initializer for {domModel.Name} -> {readModel.Name} : Domain model not in read model hierarchy");
        }

        var projectionCreator = GetGenericMethod(nameof(CreateMemberInitializerExpression), [domModel, readModel], 2);
        return projectionCreator.Invoke(null, [subTypeBindings, translations]) as LambdaExpression ?? throw new FilterParserException($"Could not build member initializer for {domModel.Name} -> {readModel.Name}");
    }

    /// <summary>
    /// Builds a member initializer for dynamically converting between two types.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="subTypeBindings"></param>
    /// <param name="translations"></param>
    /// <returns></returns>
    public static Expression<Func<TIn, TOut>> CreateMemberInitializerExpression<TIn, TOut>(IReadOnlyDictionary<Tuple<Type, Type>, LambdaExpression> subTypeBindings, Dictionary<string, string> translations) where TOut : class, TIn, new()
    {
        var param = Expression.Parameter(typeof(TIn), typeof(TIn).PrettyParameterName());

        // Valid use of null suppression, due to the new() constraint on TOut.
        var constructorInfo = typeof(TOut).GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder : null, types : Type.EmptyTypes, modifiers : null);
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

            var sourceMemberType = typeof(TIn).GetMembers(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == (otherName ?? member.Name) && m is FieldInfo or PropertyInfo) switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => throw new FilterExecutionException($"Error creating Member initializer for {typeof(TIn).Name}, no matching member for {member.Name}")
            };

            var targetMemberType = member switch {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => throw new FilterExecutionException($"Error creating Member initializer for {typeof(TOut).Name}")
            };

            if (targetMemberType.GetEnumerableContentType() is { } enumerableTargetContentType && sourceMemberType.GetEnumerableContentType() is { } enumerableSourceContentType)
            {
                bool hasSubTypeBinding = subTypeBindings.TryGetValue(new Tuple<Type, Type>(enumerableSourceContentType, enumerableTargetContentType), out var enumerableSubTypeBinding);

                var resultType = hasSubTypeBinding ? enumerableSubTypeBinding!.ReturnType : enumerableTargetContentType;
                var selectProjection = hasSubTypeBinding ? enumerableSubTypeBinding! : CreateIdentityExpression(resultType);

                var selectMethod = typeof(Enumerable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m is { Name: nameof(Enumerable.Select), IsGenericMethodDefinition: true })
                    .First(m => m.GetParameters() is [{ ParameterType.IsGenericType: true } p1, { ParameterType.IsGenericType: true } p2] && p1.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) && p2.ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                    .MakeGenericMethod(enumerableSourceContentType, resultType);

                valueExpression = Expression.Call(null, selectMethod, valueExpression, selectProjection);
            }
            else if (subTypeBindings.TryGetValue(new Tuple<Type, Type>(sourceMemberType, targetMemberType), out var bindingExpression))
            {
                valueExpression = new ParameterReplacer(bindingExpression.Parameters.First(), valueExpression).Visit(bindingExpression.Body);
            }

            memberBindings.Add(Expression.Bind(member, valueExpression));
        }
        
        var memberExpression = Expression.MemberInit(ctorExpression, memberBindings);

        return Expression.Lambda<Func<TIn, TOut>>(memberExpression, false, param);
    }

    /// <inheritdoc cref="CreateIdentityExpression{T}"/>
    public static LambdaExpression CreateIdentityExpression(Type type)
    {
        var param = Expression.Parameter(type, type.PrettyParameterName());
        return Expression.Lambda(param, param);
    }

    /// <summary>
    /// Rebuilds the LambdaExpression as an accessor if it matches the types.
    /// Throws <see cref="FilterExecutionException"/> if not.
    /// </summary>
    public static IQueryable<T> IncludeByAccessor<T>(this IQueryable<T> queryable, LambdaExpression expression)
    {
        return queryable.ApplyAccessor<IQueryable<T>, IQueryable<T>, T>(nameof(EntityFrameworkQueryableExtensions.Include), typeof(EntityFrameworkQueryableExtensions), expression);
    }

    /// <summary>
    /// 
    /// </summary>
    public static IOrderedQueryable<T> OrderByAccessor<T>(this IQueryable<T> queryable, LambdaExpression expression)
    {
        return queryable.ApplyAccessor<IOrderedQueryable<T>, IQueryable<T>, T>(nameof(Queryable.OrderByDescending), typeof(Queryable), expression);
    }

    /// <summary>
    /// 
    /// </summary>
    public static IOrderedQueryable<T> ThenOrderByAccessor<T>(this IOrderedQueryable<T> queryable , LambdaExpression expression)
    {
        return queryable.ApplyAccessor<IOrderedQueryable<T>, IOrderedQueryable<T>, T>(nameof(Queryable.ThenBy), typeof(Queryable), expression);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOut"></typeparam>
    /// <typeparam name="TIn"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static IQueryable<TOut> ApplyProjection<TOut, TIn>(this IQueryable<TIn> queryable, LambdaExpression expression) where TOut : TIn
    {
        return queryable.ApplyAccessor<IQueryable<TOut>, IQueryable<TIn>, TIn>(nameof(Queryable.Select), typeof(Queryable), expression);
    }

    /// <summary>
    /// Applies a join to a queryable.
    /// </summary>
    /// <typeparam name="TOut">The type contained in the queryable.</typeparam>
    /// <param name="queryable">The queryable to apply the join to.</param>
    /// <param name="other">The other queryable to join with.</param>
    /// <param name="keySelector">The selector expression for the key on <typeparamref name="TOut"/>.</param>
    /// <param name="otherKeySelector">The selector expression for the key on the queryable to join with.</param>
    /// <param name="combinator">Combinator expression to create a new <typeparamref name="TOut"/> from an element of the collection and other.</param>
    public static IQueryable<TOut> ApplyJoin<TOut>(this IQueryable<TOut> queryable, IQueryable other, LambdaExpression keySelector, LambdaExpression otherKeySelector, LambdaExpression combinator)
    {
        var otherType = other.GetType().GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>)).GetGenericArguments().First();

        if (keySelector.ReturnType != otherKeySelector.ReturnType && otherKeySelector.ReturnType != Nullable.GetUnderlyingType(keySelector.ReturnType))
        {
            throw new FilterExecutionException($"Could not create join query, type mismatch in key {keySelector.ReturnType} != {otherKeySelector.ReturnType} for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }

        if (typeof(TOut) != keySelector.Parameters.First().Type)
        {
            throw new FilterExecutionException($"Could not create join query, type mismatch in key selector input {typeof(TOut).FullName} != {keySelector.Parameters.First().Type.FullName} for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }

        if (otherType != otherKeySelector.Parameters.First().Type)
        {
            throw new FilterExecutionException($"Could not create join query, type mismatch in other key selector input {otherType.FullName} != {otherKeySelector.Parameters.First().Type.FullName} for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }
        
        var keyType = keySelector.ReturnType;

        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m is { Name: nameof(Queryable.Join), IsGenericMethodDefinition: true })
            .First(m =>
            {
                ParameterInfo[] ps = m.GetParameters();
                return ps is [{ ParameterType.IsGenericType: true }, { ParameterType.IsGenericType: true }, _, _, _]
                       && ps[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>)
                       && ps[1].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            }).MakeGenericMethod(typeof(TOut), otherType, keyType, typeof(TOut));

        if (method.Invoke(null, [queryable, other, keySelector, otherKeySelector, combinator]) is not IQueryable<TOut> result)
        {
            throw new FilterExecutionException($"Could not create join query for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
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

    private static string PrettyParameterName(this Type type)
    {
        return type.ShortDisplayName() + "_param";
    }
}