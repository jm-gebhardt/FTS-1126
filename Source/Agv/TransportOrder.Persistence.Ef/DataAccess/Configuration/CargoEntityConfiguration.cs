using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <inheritdoc />  
public class CargoEntityConfiguration : IEntityTypeConfiguration<Cargo>
{
    private const string ShadowIdName = "CargoId";

    /// <inheritdoc />  
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.HasKey(e => e.CorrelationId);

        builder.HasOne<Domain.Models.Agv.Agv>()
            .WithMany()
            .HasForeignKey(cargo => cargo.AgvCorrelationId)
            .HasPrincipalKey(agv => agv.CorrelationId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(c => c.CenterPointPosition)
            .WithOne()
            .HasForeignKey<Point3D>(ShadowIdName)
            .OnDelete(DeleteBehavior.ClientCascade);
        builder.Navigation(p => p.CenterPointPosition).AutoInclude();

        builder.HasOne(c => c.Dimension)
            .WithOne()
            .HasForeignKey<Cuboid>(ShadowIdName)
            .OnDelete(DeleteBehavior.ClientCascade);
        builder.Navigation(p => p.Dimension).AutoInclude();
        builder.ConfigureAllProperties(except: new[] {nameof(Cargo.CenterPointPosition), nameof(Cargo.Dimension)});
    }
}