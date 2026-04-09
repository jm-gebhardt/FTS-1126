using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.BackgroundEditor;

/// <inheritdoc />
public class BackgroundObjectPointEntityConfiguration : IEntityTypeConfiguration<BackgroundObjectPoint>
{
    private const string TableName = "BackgroundObjectPoints";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BackgroundObjectPoint> builder)
    {
        builder.HasKey(e => e.Id);
        builder.ToTable(TableName);

        builder.Property(e => e.ObjectId).IsRequired();
        builder.Property(e => e.Index).IsRequired();
        builder.Property(e => e.X).IsRequired();
        builder.Property(e => e.Y).IsRequired();

        builder.HasIndex(e => new { e.ObjectId, e.Index });
    }
}
