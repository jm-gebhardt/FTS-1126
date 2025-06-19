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
        builder.HasKey(e => e.Station);
        builder.Property(e => e.LastStateChangeTime);
        builder.Property(e => e.Station).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.State);
    }
}