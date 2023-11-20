using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

public class CargoClassMap : ClassMap<Cargo>
{
    private const string ShadowIdName = "CargoId";

    protected override void Configure(EntityTypeBuilder<Cargo> entity, ModelBuilder model)
    {
        entity.HasKey(e => e.CorrelationId);

        entity.HasOne<Domain.Models.Agv.Agv>()
            .WithMany()
            .HasForeignKey(cargo => cargo.AgvCorrelationId)
            .HasPrincipalKey(agv => agv.CorrelationId)
            .OnDelete(DeleteBehavior.ClientCascade);

        entity.HasOne(c => c.CenterPointPosition)
            .WithOne()
            .HasForeignKey<Point3D>(ShadowIdName)
            .OnDelete(DeleteBehavior.ClientCascade);
        entity.Navigation(p => p.CenterPointPosition).AutoInclude();
        EntityTypeBuilder<Point3D> pointBuilder = model.Entity<Point3D>();
        pointBuilder.Property<Guid>(ShadowIdName).ValueGeneratedOnAdd();
        pointBuilder.HasKey(ShadowIdName);
        pointBuilder.ToTable(nameof(Cargo) + nameof(Cargo.CenterPointPosition));

        entity.HasOne(c => c.Dimension)
            .WithOne()
            .HasForeignKey<Cuboid>(ShadowIdName)
            .OnDelete(DeleteBehavior.ClientCascade);
        entity.Navigation(p => p.Dimension).AutoInclude();
        EntityTypeBuilder<Cuboid> cuboidBuilder = model.Entity<Cuboid>();
        cuboidBuilder.Property<Guid>(ShadowIdName).ValueGeneratedOnAdd();
        cuboidBuilder.HasKey(ShadowIdName);
        cuboidBuilder.ToTable(nameof(Cargo) + nameof(Cargo.Dimension));

        entity.ConfigureAllProperties(except: new[] {nameof(Cargo.CenterPointPosition), nameof(Cargo.Dimension)});
    }
}