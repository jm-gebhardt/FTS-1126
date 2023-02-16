using Gebhardt.StoreWare.Agv.Common.Configurations.SettingsProviders;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs
{
    /// <inheritdoc/>
    public class AgvSettingsProvider : IAgvSettingsProvider
    {
        private readonly ISagaSettingsProvider<AgvSettings, AgvSaga> _sagaSettingsProvider;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="sagaSettingsProvider"></param>
        public AgvSettingsProvider(ISagaSettingsProvider<AgvSettings, AgvSaga> sagaSettingsProvider)
        {
            _sagaSettingsProvider = sagaSettingsProvider;
        }

        /// <inheritdoc/>
        public AgvSettings GetSettingsForAgv(Guid agvId) => _sagaSettingsProvider.GetSettings(agvId);
    }
}