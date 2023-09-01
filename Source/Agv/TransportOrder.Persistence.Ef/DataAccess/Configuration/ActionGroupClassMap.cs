using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TransportOrder.Persistence.Ef.DataAccess.Configuration;

/// <summary>
/// <see cref="ActionGroup"/> Entity Framework Mapping
/// </summary>
public class ActionGroupClassMap : ClassMap<ActionGroup>
{
    private const int MaxStateLength = 50;

    /// <summary>
    /// <see cref="ActionGroup"/> Entity Framework Mapping
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<ActionGroup> entity, ModelBuilder model)
    {
        entity.HasKey(g => g.CorrelationId);
        entity.Property(g => g.State).HasMaxLength(MaxStateLength);

        entity.HasMany(g => g.Actions)
            .WithOne()
            .HasForeignKey(a => a.ParentId)
            .HasPrincipalKey(g => g.CorrelationId)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}