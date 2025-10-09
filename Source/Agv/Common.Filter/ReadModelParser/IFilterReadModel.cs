namespace Gebhardt.Acc.Common.Filter.ReadModelParser;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TReadModel"></typeparam>
public interface IFilterReadModel<TEntity, TReadModel> : IFilterReadModel where TReadModel : class, TEntity, IFilterReadModel<TEntity, TReadModel>, new() where TEntity : class;

/// <summary>
/// Marker interface to signify that this object is a read model.
/// </summary>
public interface IFilterReadModel;