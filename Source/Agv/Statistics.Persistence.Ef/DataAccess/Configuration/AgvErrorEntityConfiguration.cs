using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvErrorEntityConfiguration : IEntityTypeConfiguration<AgvError>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvError> builder)
    {
        builder.ToTable("StatisticsAgvErrors");
        builder.HasKey(e => e.ErrorId);
        builder.Property(e => e.GridCellId);
        builder.Property(o => o.ErrorCode).HasMaxLength(DbStringLimits.Description);
        builder.Property(o => o.ErrorType).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.ErrorCount);
        builder.ConfigureAllProperties(except: [nameof(AgvError.GridCell), nameof(AgvError.ErrorCode), nameof(AgvError.ErrorType)]);
    }
}