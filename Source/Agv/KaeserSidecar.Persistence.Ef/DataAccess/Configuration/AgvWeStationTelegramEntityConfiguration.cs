using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />>
public class AgvWeStationTelegramEntityConfiguration: IEntityTypeConfiguration<AgvWeStationTelegram>
{
    private const string TableName = "AgvWeStationTelegrams";

    /// <inheritdoc />>
    public void Configure(EntityTypeBuilder<AgvWeStationTelegram> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Function).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Message).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.QueuedDate);
    }
}