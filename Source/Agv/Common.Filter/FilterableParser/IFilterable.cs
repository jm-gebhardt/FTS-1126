using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.FilterableParser;

/// <summary>
/// Marker interface to signify that this object defines a filtering of <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The entity that this read model is defined for.</typeparam>
/// <typeparam name="TFilterable">F-Bound polymorphism self reference.</typeparam>
[PublicAPI]
public interface IFilterable<TEntity, TFilterable> : IFilterable where TFilterable : class, TEntity, IFilterable<TEntity, TFilterable>, new() where TEntity : class;

/// <summary>
/// Marker interface to signify that this object defines a filtering.
/// </summary>
[PublicAPI]
public interface IFilterable;