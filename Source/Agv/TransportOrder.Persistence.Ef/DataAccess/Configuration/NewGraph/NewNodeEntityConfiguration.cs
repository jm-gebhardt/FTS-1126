using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <summary>
/// 
/// </summary>
public class NewNodeEntityConfiguration : IEntityTypeConfiguration<Node>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        builder.ToTable("New" + nameof(Node));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LevelId);
        builder.Property(e => e.X);
        builder.Property(e => e.Y);
        builder.Ignore(e => e.Children);
        builder.HasMany(e => e.IncomingEdges);
        builder.HasMany(e => e.OutgoingEdges);
        builder.Property(e => e.Name).HasMaxLength(DbStringLimits.Name);
        builder.Property(e => e.Description).HasMaxLength(DbStringLimits.Description);
    }
}