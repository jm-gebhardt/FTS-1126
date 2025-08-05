using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc/>>
public class OrderReportEntityConfiguration: IEntityTypeConfiguration<OrderReport>
{
    private const string TableName = "OrderReport";

    /// <inheritdoc/>>
    public void Configure(EntityTypeBuilder<OrderReport> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.HostOrderId);
        builder.Property(e => e.HostOrderId);
        builder.Property(e => e.Station).HasMaxLength(DbStringLimits.NodeId).IsCaseSensitive();
    }
}