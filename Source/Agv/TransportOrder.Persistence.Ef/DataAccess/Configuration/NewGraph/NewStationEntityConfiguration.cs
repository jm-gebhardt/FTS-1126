using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class NewStationEntityConfiguration : IEntityTypeConfiguration<Station>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("New" + nameof(Station));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.ShortName).HasMaxLength(DbStringLimits.Description);

        builder.Property(e => e.LifStationId).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.X);
        builder.Property(e => e.Y);
        builder.Property(e => e.Theta);
        builder.Property(e => e.StationHeight);

        builder.Ignore(e => e.InteractionNodeIds);

        builder.HasMany(e => e.InteractionNodes)
            .WithMany()
            .UsingEntity(j => j.ToTable("NewStationInteractionNodes"));
    }
}