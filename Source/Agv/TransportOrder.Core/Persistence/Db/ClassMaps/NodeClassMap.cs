using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps;

/// <summary>
/// Nodes classmap
/// </summary>
public class NodeClassMap: ClassMap<Node>
{
    /// <summary>
    /// Nodes configuration
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<Node> entity, ModelBuilder model)
    {
        // Nothing here so far, this gets configured indirect by EdgeClassMap
    }
}