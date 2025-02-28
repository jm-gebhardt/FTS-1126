using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess.Configuration;

/// <inheritdoc />
public class SystemStatusEntityConfiguration: IEntityTypeConfiguration<SystemStatus>
{
    private const string SystemStatusTable = "PrismatSystemStatus";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SystemStatus> builder)
    {
        builder.ToTable(SystemStatusTable);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ErrorInfo).HasMaxLength(DbStringLimits.ErrorInformation);
        builder.Property(e => e.IsReady);
        builder.Property(e => e.ErrorCode);
    }
}