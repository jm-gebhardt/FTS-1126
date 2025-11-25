using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvFieldViolationEntityConfiguration : IEntityTypeConfiguration<AgvFieldViolation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvFieldViolation> builder)
    {
        builder.ToTable("StatisticsAgvFieldViolations");
        builder.HasKey(e => e.ViolationId);
        builder.Property(e => e.GridCellId);
        builder.Property(e => e.ViolationCount);
        builder.ConfigureAllProperties(except: [nameof(AgvFieldViolation.GridCell)]);
    }
}