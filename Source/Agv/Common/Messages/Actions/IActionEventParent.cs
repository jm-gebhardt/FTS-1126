
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions
{
    /// <summary>
    /// Used to identify that an event belongs to a parent object
    /// </summary>
    public interface IActionEventParent : CorrelatedBy<Guid>
    {
        
    }
}
