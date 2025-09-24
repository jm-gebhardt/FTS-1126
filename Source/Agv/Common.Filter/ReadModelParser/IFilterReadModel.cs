using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Filter.ReadModelParser;

/// <summary>
/// Marker interface to signify that this object is a ReadModel of <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The entity that this read model is defined for.</typeparam>
/// <typeparam name="TReadModel">F-Bound polymorphism self reference.</typeparam>
[PublicAPI]
public interface IFilterReadModel<TEntity, TReadModel> : IFilterReadModel where TReadModel : class, TEntity, IFilterReadModel<TEntity, TReadModel>, new() where TEntity : class;

/// <summary>
/// Marker interface to signify that this object is a read model.
/// </summary>
[PublicAPI]
public interface IFilterReadModel;