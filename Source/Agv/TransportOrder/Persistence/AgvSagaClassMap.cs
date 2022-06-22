using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence
{
    /// <summary>
    /// <see cref="AgvConsumerSaga"/> Entity Framework Mapping
    /// </summary>
    public class AgvSagaClassMap : SagaClassMap<AgvConsumerSaga>
    {
        private const string AgvSagaTableName = "AgvSaga";
        private const int CurrenStateMaxLength = 64;
        
        /// <summary>
        /// <see cref="AgvConsumerSaga" /> entity configurations
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        protected override void Configure(EntityTypeBuilder<AgvConsumerSaga> entity, ModelBuilder model)
        {
            // set table names
            entity.ToTable(AgvSagaTableName);
            entity.ConfigureAllProperties();
            
            // Store CurrenState as string with max length 64
            entity.Property( x => x.CurrentState)
                .HasConversion(new EnumToStringConverter<AgvState>())
                .HasMaxLength(CurrenStateMaxLength);
        }
    }
}
