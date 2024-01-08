using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />
public class CuboidEntityConfiguration : IEntityTypeConfiguration<Cuboid>
{
    private const string ShadowIdName = "CargoId";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Cuboid> builder)
    {
        builder.Property<Guid>(ShadowIdName).ValueGeneratedOnAdd();
        builder.HasKey(ShadowIdName);
        builder.ToTable(nameof(Cargo) + nameof(Cargo.Dimension));
    }
}