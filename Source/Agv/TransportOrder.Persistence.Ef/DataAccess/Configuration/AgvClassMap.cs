using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// EF configuration or an Agv.
/// </summary>
public class AgvClassMap : ClassMap<Domain.Models.Agv.Agv>
{
    protected override void Configure(EntityTypeBuilder<Domain.Models.Agv.Agv> entity, ModelBuilder model)
    {
        entity.HasKey(g => g.CorrelationId);

        entity.ConfigureAllProperties(p => p.CanWrite && p.PropertyType != typeof(AgvStatus));

        entity.OwnsOne(e => e.AgvStatus, o =>
        {
            o.ToTable(nameof(AgvStatus));
            o.WithOwner().HasForeignKey(s => s.CorrelationId);
            o.HasKey(s => s.CorrelationId);
            foreach (var propertyInfo in typeof(AgvStatus).GetProperties())
            {
                if (propertyInfo.Name == nameof(AgvStatus.CorrelationId))
                {
                    continue;
                }
                o.Property(propertyInfo.PropertyType, propertyInfo.Name);
            }
        });
        entity.Navigation(e => e.AgvStatus).AutoInclude();
    }
}