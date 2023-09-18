using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="AgvSaga"/> Entity Framework Mapping
/// </summary>
public class AgvSagaClassMap : SagaClassMap<AgvSaga>
{
    private const string AgvSagaTableName = "AgvSaga";
    private const int CurrentStateMaxLength = 64;
        
    /// <summary>
    /// <see cref="AgvSaga" /> entity configurations
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<AgvSaga> entity, ModelBuilder model)
    {
        // set table names
        entity.ToTable(AgvSagaTableName);
        entity.ConfigureAllProperties(p => p.CanWrite);
        entity.Property(x => x.AgvStatus).HasJsonConversion();
            
        // Restrict CurrentState length
        entity.Property(x => x.CurrentState).HasMaxLength(CurrentStateMaxLength);
    }
}