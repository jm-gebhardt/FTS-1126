using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess.Configuration.NewGraph;

/// <inheritdoc/>
public class RoutingEdgeEntityConfiguration : IEntityTypeConfiguration<RoutingEdge>
{
    private const string Discriminator = $"{nameof(RoutingResource)}Type";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RoutingEdge> builder)
    {
        builder.HasDiscriminator<string>(Discriminator)
            .HasValue<RoutingEdge>(nameof(RoutingEdge))
            .HasValue<ActionRoutingEdge>(nameof(ActionRoutingEdge));

        builder.Property(Discriminator).HasMaxLength(DbStringLimits.Enum);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasOne<AgvType>()
            .WithMany()
            .HasForeignKey(e => e.AgvTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<RoutingNode>()
            .WithMany()
            .HasForeignKey(e => e.StartNodeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne<RoutingNode>()
            .WithMany()
            .HasForeignKey(e => e.EndNodeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.Property(e => e.Length);
        builder.Property(e => e.VehicleOrientation);
        builder.Property(e => e.OrientationType);
        builder.Property(e => e.RotationAtStart);
        builder.Property(e => e.RotationAtEnd);
        builder.Property(e => e.RotationAllowed);
        builder.Property(e => e.MaxSpeed);
        builder.Property(e => e.MaxRotationSpeed);

        builder.OwnsOne(e => e.Trajectory, trajectoryBuilder =>
        {
            trajectoryBuilder.ToTable("RoutingEdgeTrajectory");

            trajectoryBuilder.Property(t => t.Degree);

            trajectoryBuilder.Property(t => t.KnotVector)
                .Metadata.SetValueComparer(EfValueConverters.CreateArrayComparer<double>());

            trajectoryBuilder.OwnsMany(t => t.ControlPoints, controlPointBuilder =>
            {
                controlPointBuilder.ToTable("RoutingEdgeTrajectoryControlPoint");

                controlPointBuilder.Property(c => c.X);
                controlPointBuilder.Property(c => c.Y);
                controlPointBuilder.Property(c => c.Weight);
            });
        });
    }
}