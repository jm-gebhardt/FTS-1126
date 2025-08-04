using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class MapBackgroundEntityConfiguration : IEntityTypeConfiguration<MapBackground>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MapBackground> builder)
    {
        builder.HasKey(b => b.CorrelationId);
        builder.Ignore(b => b.IsCustom);
    }
}