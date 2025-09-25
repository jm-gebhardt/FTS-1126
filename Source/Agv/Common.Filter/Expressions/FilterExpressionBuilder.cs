using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;

namespace Gebhardt.Acc.Common.Filter.Expressions;

/// <summary>
/// Contains helper functions for generating filter expressions. Often in strongly and weakly typed variants.
/// </summary>
internal static class FilterExpressionBuilder
{
    private static MethodInfo GetGenericMethod(string name, Type[] generics, int? paramCount = null) => typeof(FilterExpressionBuilder).GetGenericMethod(name, generics, paramCount);

    /// <summary>
    /// Creates a predicate that always evaluates to true.
    /// </summary>
    /// <param name="discardedInput">Type of the input parameter. Will not be used by the resulting lambda expression.</param>
    public static LambdaExpression CreateConstantTruePredicate(Type discardedInput) => Expression.Lambda(Expression.Constant(true, typeof(bool)), false, Expression.Parameter(discardedInput, "_"));

    /// <inheritdoc cref="CreateConstantTruePredicate"/>
    private static Expression<Func<T, bool>> CreateConstantTruePredicate<T>() => Expression.Lambda<Func<T, bool>>(Expression.Constant(true, typeof(bool)), false, Expression.Parameter(typeof(T), "_"));

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
    /// Creates a property access expression that also casts the result of the property access with a cast to <typeparamref name="TProperty"/>.
    /// </summary>
    /// <typeparam name="TInstance">The type of the instance that the property will be accessed on.</typeparam>
    /// <typeparam name="TProperty">The type to cast the property to.</typeparam>
    /// <param name="propertyName">The name of the property.</param>
    public static Expression<Func<TInstance, TProperty>> CreatePropertyAccessWithCast<TInstance, TProperty>(string propertyName)
    {
        return CreatePropertyAccess<TInstance, TProperty>(propertyName, true);
    }

    /// <summary>
    /// Creates a null-coalescing property accessor for structs. Structs containing ref types are not supported.
    /// </summary>
    /// <typeparam name="TInstance">The struct type to define the property accessor for.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    public static Expression<Func<TInstance?, TProperty?>> CreateCoalescingStructAccessor<TInstance, TProperty>(string propertyName) where TInstance : struct where TProperty : struct
    {
        Expression<Func<TInstance?, bool>> nullcheck = CreatePropertyAccess<TInstance?, bool>(nameof(Nullable<TInstance>.HasValue));
        Expression<Func<TInstance?, TInstance>> valueAccess = CreatePropertyAccess<TInstance?, TInstance>(nameof(Nullable<TInstance>.Value));
        Expression<Func<TInstance, TProperty?>> propAccess = CreatePropertyAccessWithCast<TInstance, TProperty?>(propertyName);

        Expression<Func<TInstance?, TProperty?>> composed = ExpressionHelper.Compose(valueAccess, propAccess);

        var param = nullcheck.Parameters.First();

        var propAccessBody = composed.Body.ReplaceParameter(valueAccess.Parameters.First(), param);

        var nullValue = Expression.Constant(null, typeof(TProperty?));

        var condition = Expression.Condition(nullcheck.Body, propAccessBody, nullValue);

        return Expression.Lambda<Func<TInstance?, TProperty?>>(condition, false, param);
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

        return ExpressionHelper.Compose(propAccess, compareCall);
    }

    private static LambdaExpression CreateCompareToExpression(Type instance, Type property, object other, string propertyName, LambdaExpression? accessor = null)
    {
        return GetGenericMethod(nameof(CreateCompareToExpression), [instance, property], 3).Invoke(null, [other, propertyName, accessor]) as LambdaExpression ?? throw new FilterBuilderException($"Could not build compare to for {propertyName} on {instance.Name}");
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

        var left = predicate1.Body.ReplaceParameter(predicate1.Parameters.First(), param);
        var right = predicate2.Body.ReplaceParameter(predicate2.Parameters.First(), param);

        var combination = op switch
        {
            LogicalCombinationVariant.And => Expression.AndAlso(left, right),
            LogicalCombinationVariant.Or => Expression.OrElse(left, right),
            _ => throw new FilterBuilderException($"Could not find logical operator for combining filters {op.ToString()}")
        };

        return Expression.Lambda<Func<T, bool>>(combination, param);
    }

    /// <summary>
    /// Creates a predicate to test if the property on an object is one of the values in a collection.
    /// </summary>
    /// <typeparam name="TInstance">The type of the instance that the property will be accessed on.</typeparam>
    /// <typeparam name="TProperty">The type of the property on the instance.</typeparam>
    /// <param name="collection">The collection of values.</param>
    /// <param name="parameterName">The name of the property on the instance.</param>
    public static Expression<Func<TInstance, bool>> CreateIsOneOfPredicate<TInstance, TProperty>(ICollection<TProperty> collection, string parameterName)
    {
        Expression<Func<TInstance, TProperty>> accessor = CreatePropertyAccess<TInstance, TProperty>(parameterName);

        var param = Expression.Parameter(typeof(TProperty), typeof(TProperty).PrettyParameterName());

        var collectionConst = Expression.Constant(collection, typeof(ICollection<TProperty>));

        var methodInfo = typeof(ICollection<TProperty>).GetMethod(nameof(ICollection<TProperty>.Contains), [typeof(TProperty)]) ?? throw new FilterBuilderException($"Collection {collection} contains no definition of contains");

        var call = Expression.Call(collectionConst, methodInfo, param);

        Expression<Func<TProperty, bool>> lambda = Expression.Lambda<Func<TProperty, bool>>(call, param);

        return ExpressionHelper.Compose(accessor, lambda);
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

        return ExpressionHelper.Compose(compareExpression, result);
    }

    private static LambdaExpression GeneratePredicate(Type input, LambdaExpression compareExpression, ComparisonVariant variant)
    {
        return GetGenericMethod(nameof(GeneratePredicate), [input], 2).Invoke(null, [compareExpression, variant]) as LambdaExpression ?? throw new FilterBuilderException($"Could not create predicate for {input.Name} and comparison {variant.ToString()}");
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

        return ExpressionHelper.Compose(propertyAccess, compareLambda);
    }

    /// <inheritdoc cref="GeneratePredicate{T,TP}(TP,string,ComparisonVariant,Expression{Func{T, TP}}?)"/>
    public static LambdaExpression GeneratePredicate(Type inputType, Type otherType, object? other, string name, ComparisonVariant comparison, LambdaExpression? accessor, bool isNullable)
    {
        if (other?.GetType() != otherType && other?.GetType() != Nullable.GetUnderlyingType(otherType))
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
        bool noAccessorNeeded = typeof(T) == typeof(TProperty) && propertyName == string.Empty;

        if (!noAccessorNeeded)
        {
            accessor ??= CreatePropertyAccess<T, TProperty>(propertyName);
        }

        bool isNullableValueType = Nullable.GetUnderlyingType(typeof(TProperty)) != null;
        var baseType = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);
        var compType = typeof(IComparable<>).MakeGenericType(baseType);

        bool isComparable = baseType.IsAssignableTo(compType);
        bool isEqualityComparison = variant is ComparisonVariant.Equals or ComparisonVariant.Neq;

        bool canCompare = isComparable || isEqualityComparison;

        if (!canCompare)
        {
            throw new FilterExecutionException($"Cannot build comparison predicate for type {typeof(TProperty).FullName} and comparison {variant.ToString()}");
        }

        if (isNullableValueType)
        {
            return CreateNullsafeComparisonPredicate(other, propertyName, variant, accessor);
        }

        if (isEqualityComparison && !isComparable)
        {
            Expression<Func<T, bool>> predicate = GenerateEqualsPredicate(other, propertyName, accessor);

            return variant is ComparisonVariant.Neq ? NegatePredicate(predicate) : predicate;
        }

        Expression<Func<T, int>> comparison = CreateCompareToExpression(other, propertyName, accessor);

        return GeneratePredicate(comparison, variant);
    }

    /// <summary>
    /// Creates a comparison on a nullable value type.
    /// </summary>
    /// <typeparam name="T">The base type that the nullable type is on.</typeparam>
    /// <typeparam name="TProperty">The nullable property type.</typeparam>
    /// <param name="other">The object to compare to.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="variant">The variant of the comparison. Any non-equality comparison always returns null if null is one of the operands.</param>
    /// <param name="accessor">Accessor to get to <typeparamref name="TProperty"/> from <typeparamref name="T"/>.</param>
    private static Expression<Func<T, bool>> CreateNullsafeComparisonPredicate<T, TProperty>(TProperty other, string propertyName, ComparisonVariant variant, Expression<Func<T, TProperty>>? accessor)
    {
        if (other == null)
        {
            return CreateIsNullPredicate(variant, propertyName, accessor);
        }

        bool noAccessorNeeded = typeof(T) == typeof(TProperty) && propertyName == string.Empty;
        
        var baseType = Nullable.GetUnderlyingType(typeof(TProperty?)) ?? typeof(TProperty);

        Expression<Func<T, bool>> nullcheck = noAccessorNeeded ? CreateNotNullCheck<T>() : ExpressionHelper.Compose(accessor!, CreateNotNullCheck<TProperty?>());

        var param = nullcheck.Parameters.First();

        var nonNullAccessor = noAccessorNeeded ?  null : Expression.Lambda(Expression.Convert(accessor!.Body, baseType), false, accessor.Parameters.First());
        var compTo = CreateCompareToExpression(typeof(T), baseType, other, propertyName, nonNullAccessor);
        compTo = GeneratePredicate(typeof(T), compTo, variant);

        var compToBody = compTo.Body.ReplaceParameter(compTo.Parameters.First(), param);

        var resultBody = Expression.AndAlso(nullcheck.Body, compToBody);

        return Expression.Lambda<Func<T, bool>>(resultBody, false, param);
    }

    /// <summary>
    /// Creates a nullcheck predicate that tests the incoming property on <typeparamref name="T"/> against null equality depending on the variant.
    /// </summary>
    /// <typeparam name="T">The type that holds the property.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="variant">The comparison type. Any not Equal-ish variant will always be false.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="accessor">Accessor to get to <typeparamref name="TProperty"/> from <typeparamref name="T"/>.</param>
    private static Expression<Func<T, bool>> CreateIsNullPredicate<T, TProperty>(ComparisonVariant variant, string propertyName, Expression<Func<T, TProperty>>? accessor)
    {
        bool isEqualityComparison = variant is not (ComparisonVariant.Gt or ComparisonVariant.Lt or ComparisonVariant.Like);
        bool noAccessorNeeded = typeof(T) == typeof(TProperty) && propertyName == string.Empty;
        
        // Anything compared to null that is not an equality check will be false (Mimicking the falsiness of NULL in a database context).
        if (!isEqualityComparison)
        {
            return NegatePredicate(CreateConstantTruePredicate<T>());
        }

        Expression<Func<TProperty, bool>> nullcheck = CreateNotNullCheck<TProperty>();

        if (variant is not ComparisonVariant.Neq)
        {
            nullcheck = NegatePredicate(nullcheck);
        }

        return noAccessorNeeded ? Expression.Lambda<Func<T, bool>>(nullcheck.Body, false, nullcheck.Parameters.First()) : ExpressionHelper.Compose(accessor!, nullcheck);
    }

    /// <inheritdoc cref="GeneratePredicate{T}"/>
    public static Expression<Func<T, bool>> GeneratePredicate<T, TProperty>(TProperty other, ComparisonVariant variant, Expression<Func<T, TProperty?>> accessor) where TProperty : struct
    {
        Expression<Func<TProperty?, bool>> nullcheck = CreateNotNullCheck<TProperty?>();

        Expression<Func<T, bool>> propertyNullcheck = ExpressionHelper.Compose(accessor, nullcheck);

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
    private static Expression<Func<T, bool>> GenerateEqualsPredicate<T, TProperty>(TProperty other, string propertyName, Expression<Func<T, TProperty>>? accessor = null)
    {
        accessor ??= CreatePropertyAccess<T, TProperty>(propertyName);

        var param = Expression.Parameter(typeof(TProperty), typeof(TProperty).PrettyParameterName());
        
        var otherConst = Expression.Constant(other, typeof(TProperty));

        var equalsExpression = Expression.Equal(param, otherConst);

        Expression<Func<TProperty, bool>> localPredicate = Expression.Lambda<Func<TProperty, bool>>(equalsExpression, false, param);

        return ExpressionHelper.Compose(accessor, localPredicate);
    }

    /// <summary>
    /// Creates a check to see if a value of the given type is null or not.
    /// </summary>
    /// <typeparam name="T">The type to generate the check for.</typeparam>
    private static Expression<Func<T, bool>> CreateNotNullCheck<T>()
    {
        var param = Expression.Parameter(typeof(T), typeof(T).PrettyParameterName());

        var nullConstant = Expression.Constant(null, typeof(T?));
        
        return Expression.Lambda<Func<T, bool>>(
            Expression.NotEqual(param, nullConstant),
            false,
            param
        );
    }

    /// <inheritdoc cref="CreateNotNullCheck{T}"/>
    private static LambdaExpression CreateNotNullCheck(Type type)
    {
        var nullableType = type;
        if (type.IsValueType)
        {
            nullableType = typeof(Nullable<>).MakeGenericType(type);
        }

        var param = Expression.Parameter(nullableType, nullableType.PrettyParameterName());

        var nullConstant = Expression.Constant(null, nullableType);

        return Expression.Lambda(
            Expression.NotEqual(param, nullConstant),
            false,
            param
        );
    }

    /// <summary>
    /// Creates an expression that wraps a lambda into a null-check and only calls the body of the lambda if the input is not null.
    /// </summary>
    /// <typeparam name="TIn">Input type.</typeparam>
    /// <typeparam name="TOut">Output type.</typeparam>
    /// <param name="expression">Lambda expression to wrap the parameter for.</param>
    public static Expression<Func<TIn?, TOut?>> MakeNullCoalescing<TIn, TOut>(Expression<Func<TIn?, TOut>> expression) where TIn : class
    {
        var param = expression.Parameters.First();

        Expression<Func<TIn?, bool>> nullcheck = CreateNotNullCheck<TIn?>();

        var constNull = Expression.Constant(null, typeof(TOut?));

        var nullcheckBoundParam = nullcheck.Body.ReplaceParameter(nullcheck.Parameters.First(), param);

        var condition = Expression.Condition(nullcheckBoundParam, expression.Body, constNull);

        return Expression.Lambda<Func<TIn?, TOut?>>(condition, false, param);
    }

    /// <inheritdoc cref="MakeNullCoalescing{TIn,TOut}"/>
    public static LambdaExpression MakeNullCoalescing(ParameterExpression param, Expression body, Type returnType)
    {
        var nullcheck = CreateNotNullCheck(param.Type);

        var constNull = Expression.Constant(null, returnType);

        var nullcheckBoundParam = nullcheck.Body.ReplaceParameter(nullcheck.Parameters.First(), param);

        // body might be value type so we need to convert into the Nullable<> variant
        if (body.Type != returnType)
        {
            body = Expression.Convert(body, returnType);
        }

        var condition = Expression.Condition(nullcheckBoundParam, body, constNull);

        return Expression.Lambda(condition, false, param);
    }

    /// <summary>
    /// Negates a predicate.
    /// </summary>
    /// <typeparam name="T">Input type of the predicate.</typeparam>
    /// <param name="predicate">The predicate to invert.</param>
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

    /// <summary>
    /// Rebuilds the LambdaExpression as a typed predicate if it matches the types.
    /// Throws <see cref="FilterExecutionException"/> if not.
    /// </summary>
    public static Expression<Func<T, bool>> AsPredicate<T>(this LambdaExpression expression)
    {
        return expression.AsStronglyTyped<T, bool>();
    }

    /// <inheritdoc cref="CreateAggregationPredicate{TInput}"/>
    public static LambdaExpression CreateAggregationPredicate(Type elementType, LambdaExpression elementPredicate, LambdaExpression accessor, CollectionAggregateVariant aggregateVariant)
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

        var generator = GetGenericMethod(nameof(CreateAggregationPredicate), [elementType], 2);

        var aggregation = generator.Invoke(null, [elementPredicate, aggregateVariant]) as LambdaExpression;

        var aggregationInputType = aggregation?.Parameters.FirstOrDefault()?.Type ?? outputType;

        if (!outputType.IsAssignableTo(aggregationInputType))
        {
            throw new FilterBuilderException($"Could not create aggregation predicate expression tree, accessor output {outputType} not assignable to aggregate input {aggregationInputType}");
        }

        if (aggregation == null)
        {
            throw new FilterBuilderException("Could not create aggregation predicate expression tree, aggregation could not be crated");
        }

        return ExpressionHelper.Compose(accessor, aggregation);
    }
}