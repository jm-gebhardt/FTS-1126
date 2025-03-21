using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.Telegram;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess.Configuration;

/// <inheritdoc/>>
public class TelegramEntityConfiguration: IEntityTypeConfiguration<Telegram>
{
    private const string TableName = "PrismatTelgrams";

    /// <inheritdoc/>>
    public void Configure(EntityTypeBuilder<Telegram> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Function).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.Message).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.QueuedDate);
    }
}