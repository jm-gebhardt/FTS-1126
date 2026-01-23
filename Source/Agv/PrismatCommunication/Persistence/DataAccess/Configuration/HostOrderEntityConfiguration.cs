using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess.Configuration;

/// <inheritdoc />
public class HostOrderEntityConfiguration : IEntityTypeConfiguration<HostOrder>
{
    private const string HostOrderActionsTable = "PrismatHostOrderActions";
    private const string HostOrderTable = "PrismatHostOrder";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HostOrder> builder)
    {
        builder.ToTable(HostOrderTable);
        builder.HasKey(e => e.CorrelationId);
        builder.OwnsMany(e => e.Actions, o =>
        {
            o.HasKey(e => e.CorrelationId);
            o.ToTable(HostOrderActionsTable);
            o.Property(e => e.Node).HasMaxLength(DbStringLimits.NodeName);
            o.Property(e => e.ActionGroupId);
            o.Property(e => e.ActionType).HasMaxLength(DbStringLimits.ActionType);
            o.Property(p => p.InfoToAgv).HasJsonConversion();
        });
        builder.Property(e => e.OrderId).HasMaxLength(DbStringLimits.OrderId);
        builder.Ignore(e => e.TransportOrderId);
        builder.Property(e => e.Hu).HasMaxLength(DbStringLimits.Hu);
        builder.Navigation(e => e.Actions).AutoInclude();
    }
}