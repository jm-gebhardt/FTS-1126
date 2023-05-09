using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.ClassMaps
{
    /// <summary>
    /// <see cref="Map"/> Entity Framework Mapping
    /// </summary>
    public class MapClassMap : ClassMap<Map>
    {
        private const int CurrentStateMaxLength = 32;

        /// <summary>
        /// <see cref="Map" /> entity configurations
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        protected override void Configure(EntityTypeBuilder<Map> entity, ModelBuilder model)
        {
            entity.ConfigureAllProperties();

            // Restrict CurrentState length
            entity.Property(x => x.MapId).HasMaxLength(CurrentStateMaxLength);
        }
    }
}