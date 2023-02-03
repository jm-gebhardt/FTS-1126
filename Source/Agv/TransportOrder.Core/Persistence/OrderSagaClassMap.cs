using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence
{
    /// <summary>
    /// <see cref="OrderSaga"/> Entity Framework Mapping
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class OrderSagaClassMap : SagaClassMap<OrderSaga>
    {
        private const string OrderSagaTableName = "OrderSaga";
        private const string OrderTableName = "OrderSaga_OrderDetails";
        private const string OrderActionName = "OrderSaga_OrderActions";
        private const int CurrentStateMaxLength = 64;

        /// <summary>
        /// <see cref="OrderSaga" /> entity configurations 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        protected override void Configure(EntityTypeBuilder<OrderSaga> entity, ModelBuilder model)
        {
            // set table names
            entity.ToTable(OrderSagaTableName);
            model.Entity<OrderDetails>().ToTable(OrderTableName);

            // Restrict CurrentState length
            entity.Property(x => x.CurrentState).HasMaxLength(CurrentStateMaxLength);

            // Specify Primary Keys
            entity.HasKey(x => x.CorrelationId);
            model.Entity<OrderDetails>().HasKey(o => o.CorrelationId);

            entity.Property(x => x.AssignedAgv);

            // Specify One-to-One relationship between OrderSaga and OrderDetail
            entity.HasOne(x => x.OrderDetails)
                .WithOne()
                .HasForeignKey<OrderDetails>(x => x.CorrelationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.OrderDetails).AutoInclude();

            // Add all Order properties
            model.Entity<OrderDetails>().ConfigureAllProperties();

            model.Entity<OrderDetails>().Property(x => x.Creator)
                .HasConversion(new EnumToStringConverter<HostOrderCreator>());

            model.Entity<OrderDetails>().Property(x => x.CreationDate)
                .HasColumnType("datetime2");
        }
    }
}