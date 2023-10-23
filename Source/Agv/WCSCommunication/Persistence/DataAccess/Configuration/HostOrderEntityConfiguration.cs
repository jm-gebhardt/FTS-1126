using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Models.HostOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Persistence.DataAccess.Configuration;

/// <inheritdoc />
public class HostOrderEntityConfiguration : IEntityTypeConfiguration<HostOrder>
{
    private const string HostOrderActions = "HostOrderActions";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HostOrder> builder)
    {
        builder.HasKey(e => e.CorrelationId);
        builder.OwnsMany(e => e.Actions, o =>
        {
            o.HasKey(e => e.CorrelationId);
            o.Property(e => e.ActionGroupId);
            o.ToTable(HostOrderActions);
        });
        builder.Navigation(e => e.Actions).AutoInclude();
    }
}