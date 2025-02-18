using System.Text.Json;
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.InstantActionDefinition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class InstantActionDefinitionEntityConfiguration : IEntityTypeConfiguration<InstantActionDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InstantActionDefinition> builder)
    {
        builder.HasKey(g => g.CorrelationId);

        builder.Property(e => e.ActionType).HasMaxLength(DbStringLimits.ActionType);
        builder.Property(e => e.Description).HasMaxLength(DbStringLimits.Description);
        builder.Property(e => e.ResultDescription).HasMaxLength(DbStringLimits.Description);

        builder.Property(e => e.BlockingTypes)
            .HasConversion(
                v => string.Join(',', v!),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Enum.Parse<ActionBlockingType>)
                    .ToList()
            );

        builder.Property(e => e.Parameters)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<List<AgvInstantActionParameterDefinition>>(v, (JsonSerializerOptions)null!)
            );

        builder.Property(e => e.AgvCorrelationIds)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList()
            );
    }
}
