using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence
{
    /// <summary>
    /// <see cref="OrderSaga"/> Entity Framework Mapping
    /// </summary>
    public class OrderSagaClassMap : SagaClassMap<OrderSaga>
    {
        private const string OrderSagaTableName = "OrderSaga";
        private const string OrderTableName = "OrderSaga_Order";
        private const int CurrenStateMaxLength = 64;
        
        /// <summary>
        /// <see cref="OrderSaga" /> entity configurations 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        protected override void Configure(EntityTypeBuilder<OrderSaga> entity, ModelBuilder model)
        {
            // set table names
            entity.ToTable(OrderSagaTableName);
            model.Entity<Order>().ToTable(OrderTableName);

            // Restrict CurrenState length
            entity.Property(x => x.CurrentState).HasMaxLength(CurrenStateMaxLength);
            
            // Specify Primary Keys
            entity.HasKey(x => x.CorrelationId);
            model.Entity<Order>().HasKey(o => o.CorrelationId);
            
            entity.Property(x => x.AssignedAgv);
            
            // Specify One-to-One relationship between OrderSaga and Order
            entity.HasOne(x => x.Order)
                .WithOne()
                .HasForeignKey<Order>(x => x.CorrelationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Order).AutoInclude();

            // Add all Order properties
            model.Entity<Order>().ConfigureAllProperties();

            model.Entity<Order>().Property(x => x.Creator)
                .HasConversion(new EnumToStringConverter<HostOrderCreator>());
        }
    }
}