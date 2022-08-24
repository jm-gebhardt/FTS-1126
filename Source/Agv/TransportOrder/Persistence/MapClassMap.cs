using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence
{
    /// <summary>
    /// <see cref="Map"/> Entity Framework Mapping
    /// </summary>
    public class MapClassMap : SagaClassMap<Map>
    {
        private const string MapTableName = "MapTable";
        private const int CurrentStateMaxLength = 32;

        /// <summary>
        /// <see cref="Map" /> entity configurations
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        protected override void Configure(EntityTypeBuilder<Map> entity, ModelBuilder model)
        {
            // set table names
            entity.ToTable(MapTableName);
            entity.ConfigureAllProperties();

            // Restrict CurrentState length
            entity.Property(x => x.MapId).HasMaxLength(CurrentStateMaxLength);
        }
    }
}