using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class Point3DEntityConfiguration: IEntityTypeConfiguration<Point3D>
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