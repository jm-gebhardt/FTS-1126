using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.BackgroundEditor;

/// <summary>
/// Configures the <see cref="PolylineObject"/> subtype, in particular its
/// cascade-delete relationship to <see cref="BackgroundObjectPoint"/>.
/// </summary>
public class PolylineObjectEntityConfiguration : IEntityTypeConfiguration<PolylineObject>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PolylineObject> builder)
    {
        builder.HasMany(e => e.Points)
            .WithOne()
            .HasForeignKey(nameof(BackgroundObjectPoint.ObjectId))
            .OnDelete(DeleteBehavior.Cascade);
    }
}
