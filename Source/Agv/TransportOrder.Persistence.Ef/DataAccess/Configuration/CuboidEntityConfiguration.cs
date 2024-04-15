using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cargo = Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv.Cargo;

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