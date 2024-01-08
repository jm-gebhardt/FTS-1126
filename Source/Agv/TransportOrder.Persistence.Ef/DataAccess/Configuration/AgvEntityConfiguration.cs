using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvEntityConfiguration : IEntityTypeConfiguration<Domain.Models.Agv.Agv>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Domain.Models.Agv.Agv> builder)
    {
        builder.HasKey(g => g.CorrelationId);

        builder.ConfigureAllProperties(p => p.CanWrite && p.PropertyType != typeof(AgvStatus));

        builder.OwnsOne(e => e.AgvStatus, o =>
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
        builder.Navigation(e => e.AgvStatus).AutoInclude();
    }
}