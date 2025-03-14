using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess.Configuration;

/// <inheritdoc />>
public class AgvEntityConfiguration: IEntityTypeConfiguration<Agv>
{
    private const string AgvTable = "DeviceCommunicationAgv";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Agv> builder)
    {
        builder.ToTable(AgvTable);
        builder.HasKey(e => e.CorrelationId);
        builder.Property(e => e.SerialNumber).HasMaxLength(DbStringLimits.Description);
    }
}