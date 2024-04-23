using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cargo = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.Cargo;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration;

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
        builder.ConfigureAllProperties(except: new[] { nameof(Cargo.CenterPointPosition), nameof(Cargo.Dimension) });
    }
}