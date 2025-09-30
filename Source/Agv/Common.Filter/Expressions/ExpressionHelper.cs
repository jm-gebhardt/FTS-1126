using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Gebhardt.Acc.Common.Filter.Expressions;

internal static class ExpressionHelper
{
    private static MethodInfo GetGenericMethod(string name, Type[] generics, int? paramCount = null) => typeof(ExpressionHelper).GetGenericMethod(name, generics, paramCount);

    private class ParameterReplacer(ParameterExpression oldParam, Expression replacement) : ExpressionVisitor
    {
        private ParameterExpression OldParam { get; } = oldParam;

        private Expression Replacement { get; } = replacement;

        protected override Expression VisitParameter(ParameterExpression current)
        {
            return current == OldParam ? Replacement : base.VisitParameter(current);
        }
    }

    /// <summary>
    /// Replaces all occurrences of the provided parameter expression in the provided expression with a replacement expression.
    /// </summary>
    /// <param name="body">The expression to replace in.</param>
    /// <param name="oldParameter">The parameter expression to replace.</param>
    /// <param name="replacement">The expression to replace with.</param>
    public static Expression ReplaceParameter(this Expression body, ParameterExpression oldParameter, Expression replacement)
    {
        return new ParameterReplacer(oldParameter, replacement).Visit(body);
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

        var newBody = second.Body.ReplaceParameter(second.Parameters.First(), first.Body);

        return Expression.Lambda<Func<TIn, TOut>>(newBody, param);
    }

    /// <inheritdoc cref="CreateIdentityExpression{T}"/>
    public static LambdaExpression CreateIdentityExpression(Type type)
    {
        var param = Expression.Parameter(type, type.PrettyParameterName());
        return Expression.Lambda(param, param);
    }

    /// <summary>
    /// Attempts to rebuild the lambda expression as a strongly typed lambda expression if the types and parameter count match.
    /// </summary>
    /// <typeparam name="TIn">Input type.</typeparam>
    /// <typeparam name="TOut">Output type.</typeparam>
    /// <param name="expression">The lambda expression.</param>
    /// <exception cref="FilterExecutionException">If the types/parameter count does not match.</exception>
    public static Expression<Func<TIn, TOut>> AsStronglyTyped<TIn, TOut>(this LambdaExpression expression)
    {
        if (expression is Expression<Func<TIn, TOut>> convertedExpression)
        {
            return convertedExpression;
        }

        if (expression.Parameters.Count != 1 || expression.Parameters.First().Type != typeof(TIn))
        {
            throw new FilterExecutionException($"Could not strongly type lambda to {typeof(TIn).Name} -> {typeof(TOut).Name}, Parameters: {expression.Parameters.Count}, {expression.Parameters.FirstOrDefault()?.Type.FullName}");
        }

        if (!expression.ReturnType.IsAssignableTo(typeof(TOut)))
        {
            throw new FilterExecutionException($"Could not convert lambda to {typeof(TIn).Name} -> {typeof(TOut).Name}, Return: {expression.ReturnType.Name}");
        }

        return Expression.Lambda<Func<TIn, TOut>>(expression.Body, expression.TailCall, expression.Parameters);
    }

    /// <summary>
    /// Creates a pretty parameter name for a type.
    /// </summary>
    /// <param name="type">The type to generate the name for.</param>
    public static string PrettyParameterName(this Type type)
    {
        return type.ShortDisplayName() + "_param";
    }
}