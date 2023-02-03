using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence
{
    /// <summary>
    /// <see cref="ActionSagaClassMap"/> Entity Framework Mapping
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class ActionSagaClassMap : SagaClassMap<ActionSaga>
    {
        private const string MapTableName = "Action";
        private const int CurrentStateMaxLength = 64;

        /// <summary>
        /// <see cref="ActionSagaClassMap" /> entity configurations
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="model"></param>
        protected override void Configure(EntityTypeBuilder<ActionSaga> entity, ModelBuilder model)
        {
            // set table names
            entity.ToTable(MapTableName);
            entity.ConfigureAllProperties();
            entity.Property(x => x.Action).HasJsonConversion();

            // Restrict CurrentState length
            entity.Property(x => x.CurrentState).HasMaxLength(CurrentStateMaxLength);
        }
    }
}