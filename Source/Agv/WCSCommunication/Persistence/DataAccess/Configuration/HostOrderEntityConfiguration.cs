using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.WCSCommunication.Domain.Models.HostOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.WCSCommunication.Persistence.DataAccess.Configuration;

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
            o.Property(e => e.ActionType).HasMaxLength(DbStringLimits.ActionType);
            o.Property(e => e.Node).HasMaxLength(DbStringLimits.NodeName);
            o.ToTable(HostOrderActions);
        });
        builder.Property(e => e.OrderId).HasMaxLength(DbStringLimits.OrderId);
        builder.Navigation(e => e.Actions).AutoInclude();
    }
}