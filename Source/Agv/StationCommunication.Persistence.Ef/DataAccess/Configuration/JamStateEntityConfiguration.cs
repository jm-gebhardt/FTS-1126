using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.StationCommunication.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class JamStateEntityConfiguration: IEntityTypeConfiguration<JamState>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<JamState> builder)
    {
        builder.HasKey(e => e.StationId);
        builder.Property(e => e.LastStateChangeTime);
        builder.Property(e => e.StationId).HasMaxLength(DbStringLimits.NodeId);
        builder.Property(e => e.State);
    }
}