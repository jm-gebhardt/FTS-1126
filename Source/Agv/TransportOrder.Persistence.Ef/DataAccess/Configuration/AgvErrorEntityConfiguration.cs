using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Persistence.Ef;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class AgvErrorEntityConfiguration : IEntityTypeConfiguration<AgvError>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgvError> builder)
    {
        builder.HasKey(o => o.CorrelationId);
        ValueConversionExtensions.HasJsonConversion(builder.Property(o => o.ErrorReferences));
        builder.Property(o => o.ErrorCode).HasMaxLength(DbStringLimits.Description);
        builder.Property(o => o.ErrorType).HasMaxLength(DbStringLimits.Description);
        builder.Property(o => o.Source).HasConversion<string>().HasMaxLength(DbStringLimits.Enum);
    }
}