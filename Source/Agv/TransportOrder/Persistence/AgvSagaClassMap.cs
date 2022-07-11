using Gebhardt.StoreWare.Agv.TransportOrder.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence
{
    /// <summary>
    /// <see cref="AgvSaga"/> Entity Framework Mapping
    /// </summary>
    public class AgvSagaClassMap : SagaClassMap<AgvSaga>
    {
        private const string AgvSagaTableName = "AgvSaga";
        private const int CurrenStateMaxLength = 64;
        
        /// <summary>
        /// <see cref="AgvSaga" /> entity configurations
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        protected override void Configure(EntityTypeBuilder<AgvSaga> entity, ModelBuilder model)
        {
            // set table names
            entity.ToTable(AgvSagaTableName);
            entity.ConfigureAllProperties();
            entity.Property(x => x.AgvStatus).HasJsonConversion();
            entity.Property(x => x.AgvGraph).HasJsonConversion();
            entity.Property(x => x.AgvData).HasJsonConversion();

            // Restrict CurrenState length
            entity.Property(x => x.CurrentState).HasMaxLength(CurrenStateMaxLength);
        }
    }
}
