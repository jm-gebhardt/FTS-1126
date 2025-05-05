using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc cref="IEntityTypeConfiguration{TEntity}"/>>
public class DecisionPointActionEntityConfiguration : IEntityTypeConfiguration<DecisionPointAction>
{
    /// <inheritdoc cref="IEntityTypeConfiguration{TEntity}"/>>
    public void Configure(EntityTypeBuilder<DecisionPointAction> builder)
    {
        builder.HasKey(e => e.CorrelationId);
        builder.Property(e => e.CorrelationId);
        builder.Property(e => e.AgvCorrelationId);
        builder.Property(e => e.Target).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.EntryTime);
    }
}