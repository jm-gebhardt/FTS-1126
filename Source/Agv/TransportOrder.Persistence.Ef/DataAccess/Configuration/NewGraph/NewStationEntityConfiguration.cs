using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <summary>
/// 
/// </summary>
public class NewStationEntityConfiguration : IEntityTypeConfiguration<Station>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.X);
        builder.Property(e => e.Y);
        builder.Property(e => e.StationHeight);
        builder.Property(e => e.Theta);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.Description).HasMaxLength(DbStringLimits.Description);
        builder.HasMany(e => e.InteractionNodes);
    }
}