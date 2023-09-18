using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;

/// <summary>
/// Entity Framework Mapping
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ClassMap<T> : IClassMap<T>
    where T : class
{
    /// <inheritdoc />
    public Type ClassType => typeof(T);

    /// <inheritdoc />
    public virtual string TableName => ClassType.Name;

    /// <inheritdoc />
    public void Configure(ModelBuilder model)
    {
        EntityTypeBuilder<T> entity = model.Entity<T>();
        SetTableName(entity);
        Configure(entity, model);
    }

    /// <summary>
    /// Set table name
    /// </summary>
    /// <param name="entity"></param>
    private void SetTableName(EntityTypeBuilder<T> entity)
    {
        if (TableName != string.Empty)
        {
            entity.ToTable(TableName);
        }
    }

    /// <summary>
    /// Configure the class map model
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    [UsedImplicitly]
    protected abstract void Configure(EntityTypeBuilder<T> entity, ModelBuilder model);
}