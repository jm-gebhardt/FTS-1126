using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Persistence.Ef.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvWifiMetricsEntityConfiguration : IEntityTypeConfiguration<AgvWifiMetrics>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvWifiMetrics> builder)
    {
        builder.ToTable("StatisticsAgvWifiMetrics");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.GridCellId);
        builder.Property(e => e.Ssid).HasMaxLength(StatisticsStringLimits.DefaultMetric);
        builder.Property(e => e.Band).HasMaxLength(StatisticsStringLimits.DefaultMetric);
        builder.ConfigureAllProperties(except: [nameof(AgvWifiMetrics.GridCell)]);
    }
}