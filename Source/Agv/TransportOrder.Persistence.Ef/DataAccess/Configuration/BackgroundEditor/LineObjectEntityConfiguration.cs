using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.BackgroundEditor;

/// <summary>
/// Configures the <see cref="LineObject"/> subtype, in particular its
/// cascade-delete relationship to <see cref="BackgroundObjectPoint"/>.
/// </summary>
public class LineObjectEntityConfiguration : IEntityTypeConfiguration<LineObject>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LineObject> builder)
    {
        builder.HasMany(e => e.Points)
            .WithOne()
            .HasForeignKey(nameof(BackgroundObjectPoint.ObjectId))
            .OnDelete(DeleteBehavior.Cascade);
    }
}
