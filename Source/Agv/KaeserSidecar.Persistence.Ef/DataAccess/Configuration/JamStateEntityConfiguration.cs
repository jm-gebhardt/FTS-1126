using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class JamStateEntityConfiguration: IEntityTypeConfiguration<JamState>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<JamState> builder)
    {
        // TODO: Add relation to station by using NodeId and rename to StationNodeId
        builder.HasKey(e => e.StationId);
        builder.Property(e => e.LastStateChangeTime);
        // TODO: Remove not needed
        builder.Property(e => e.StationId).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.State);
    }
}