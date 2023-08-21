using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Persistence;
using Gebhardt.StoreWare.Agv.WCSCommunication.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Persistence;

/// <summary>
/// <see cref="HostOrderSaga"/> Entity Framework Mapping
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class HostOrderSagaClassMap : SagaClassMap<HostOrderSaga>
{
    private const string HostOrderSagaTableName = "HostOrderSaga";
    private const string HostOrderDetailsTableName = "HostOrderSaga_OrderDetails";
    private const int CurrentStateMaxLength = 64;

    /// <summary>
    /// <see cref="HostOrderSaga" /> entity configurations 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="model"></param>
    protected override void Configure(EntityTypeBuilder<HostOrderSaga> entity, ModelBuilder model)
    {
        // Set table names
        entity.ToTable(HostOrderSagaTableName);
        model.Entity<OrderDetails>().ToTable(HostOrderDetailsTableName);

        // Restrict CurrentState length
        entity.Property(x => x.CurrentState).HasMaxLength(CurrentStateMaxLength);

        // Specify Primary Key
        entity.HasKey(x => x.CorrelationId);
        model.Entity<OrderDetails>().HasKey(o => o.CorrelationId);

        // Specify One-to-One relationship between HostOrderSaga and HostOrderSaga_OrderDetails
        entity.HasOne(x => x.OrderDetails)
            .WithOne()
            .HasForeignKey<OrderDetails>(x => x.CorrelationId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(x => x.OrderDetails).AutoInclude();
        entity.Property(p => p.PickActionGuid);
        entity.Property(p => p.DropActionGuid);

        // Add all Order properties
        model.Entity<OrderDetails>().ConfigureAllProperties();

        model.Entity<OrderDetails>().Property(x => x.Creator)
            .HasConversion(new EnumToStringConverter<HostOrderCreator>());

        model.Entity<OrderDetails>().Property(x => x.CreationDate)
            .HasColumnType("datetime2");
    }
}