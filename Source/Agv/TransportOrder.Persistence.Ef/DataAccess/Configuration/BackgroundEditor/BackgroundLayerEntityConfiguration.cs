using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.BackgroundEditor;

/// <inheritdoc />
public class BackgroundLayerEntityConfiguration : IEntityTypeConfiguration<BackgroundLayer>
{
    private const string TableName = "BackgroundLayers";
    private const string BackgroundLayerObjects = "BackgroundLayerObjects"; 

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BackgroundLayer> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable(TableName);

        builder.Property(e => e.BackgroundId).IsRequired();
        builder.Property(e => e.ExternalId).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Description).IsRequired();
        builder.Property(e => e.Visible).IsRequired();
        builder.Property(e => e.Locked).IsRequired();

        // Many-to-many: a layer references objects, an object can appear in multiple layers
        // ClientCascade on the object side avoids SQL Server's multiple cascade paths error
        // (Background → Layers → LayerObjects AND Background → Objects → LayerObjects).
        // EF Core handles the join-row deletion in memory when objects are tracked.
        builder.HasMany(e => e.Objects)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                BackgroundLayerObjects,
                r => r.HasOne<BackgroundObject>().WithMany().OnDelete(DeleteBehavior.ClientCascade),
                l => l.HasOne<BackgroundLayer>().WithMany().OnDelete(DeleteBehavior.Cascade));

        builder.Navigation(e => e.Objects).AutoInclude();
    }
}
