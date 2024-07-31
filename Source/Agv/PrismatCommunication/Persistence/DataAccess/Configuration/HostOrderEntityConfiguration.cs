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
            o.Property(e => e.ActionGroupId);
            o.ToTable(HostOrderActionsTable);
            o.Property(p => p.InfoToAgv).HasJsonConversion();
        });
        builder.Navigation(e => e.Actions).AutoInclude();
    }
}