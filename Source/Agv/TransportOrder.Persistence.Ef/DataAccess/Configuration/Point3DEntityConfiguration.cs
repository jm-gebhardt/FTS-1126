using Gebhardt.Acc.Common.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cargo = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Cargo;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class Point3DEntityConfiguration : IEntityTypeConfiguration<Point3D>
{
    private const string ShadowIdName = "CargoId";

    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<Point3D> builder)
    {
        builder.Property<Guid>(ShadowIdName).ValueGeneratedOnAdd();
        builder.HasKey(ShadowIdName);
        builder.ToTable(nameof(Cargo) + nameof(Cargo.CenterPointPosition));
    }
}