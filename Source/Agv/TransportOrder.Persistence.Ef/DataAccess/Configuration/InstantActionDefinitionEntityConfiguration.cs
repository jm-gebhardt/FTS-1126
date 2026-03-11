using Gebhardt.Acc.Common.Domain.Models;
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
            .HasConversion(EfValueConverters.CreateEnumListConverter<BlockingType>())
            .Metadata.SetValueComparer(EfValueConverters.CreateListComparer<BlockingType>());

        builder.Property(e => e.Parameters)
            .HasConversion(EfValueConverters.CreateJsonConverter<List<AgvInstantActionParameterDefinition>?>())
            .Metadata.SetValueComparer(EfValueConverters.CreateListComparer<AgvInstantActionParameterDefinition>());
        
        builder.Property(e => e.AgvCorrelationIds)
            .HasConversion(EfValueConverters.CreateGuidListConverter())
            .Metadata.SetValueComparer(EfValueConverters.CreateListComparer<Guid>());
    }
}
