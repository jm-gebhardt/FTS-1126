using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />>
public class AgvAplCargoStateTelegramEntityConfiguration : IEntityTypeConfiguration<AgvAplCargoStateTelegram>
{
    private const string TableName = "AgvAplCargoStateTelegrams";

    /// <inheritdoc />>
    public void Configure(EntityTypeBuilder<AgvAplCargoStateTelegram> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Function).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Message).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.QueuedDate);
    }
}