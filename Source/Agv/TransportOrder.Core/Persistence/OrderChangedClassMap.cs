using Gebhardt.StoreWare.Agv.TransportOrder.Core.Db.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence
{
    /// <summary>
    /// <see cref="OrderChangedEvent"/> Entity Framework Mapping
    /// </summary>
    public class OrderChangedClassMap : SagaClassMap<OrderChangedEvent>
    {
        private const string MapTableName = "OrderChangedEvent";
        private const int CurrentStateMaxLength = 32;

        /// <summary>
        /// <see cref="OrderChangedEvent" /> entity configurations
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        protected override void Configure(EntityTypeBuilder<OrderChangedEvent> entity, ModelBuilder model)
        {
            // set table names
            entity.ToTable(MapTableName);
            entity.ConfigureAllProperties();

            // Restrict CurrentState length
            entity.Property(x => x.State).HasMaxLength(CurrentStateMaxLength);
            entity.Property(x => x.Hook).HasMaxLength(CurrentStateMaxLength);
            entity.Property(x => x.OrderSaga).HasJsonConversion();
        }
    }
}