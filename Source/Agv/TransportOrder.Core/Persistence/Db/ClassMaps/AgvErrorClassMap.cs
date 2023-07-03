using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps;

/// <summary>
/// <see cref="AgvError" /> Entity Framework Mapping
/// </summary>
public class AgvErrorClassMap : ClassMap<AgvError>
{
    /// <inheritdoc cref="AgvErrorClassMap"/>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    /// <exception cref="NotImplementedException"></exception>
    protected override void Configure(EntityTypeBuilder<AgvError> entity, ModelBuilder model)
    {
        entity.HasKey(o => o.CorrelationId);
        entity.Property(o => o.ErrorReferences).HasJsonConversion();
    }
}