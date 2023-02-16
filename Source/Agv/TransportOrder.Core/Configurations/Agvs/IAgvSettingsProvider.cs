
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Configurations.Agvs
{
    /// <summary>
    /// Provider for the <see cref="AgvSettings"/>
    /// </summary>
    public interface IAgvSettingsProvider
    {
        /// <summary>
        /// Returns the <see cref="AgvSettings"/> for the requested Agv
        /// </summary>
        AgvSettings GetSettingsForAgv(Guid agvId);
    }
}