using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;

/// <summary>
/// Used to identify that an event belongs to a parent object
/// </summary>
public interface IActionEventParent : CorrelatedBy<Guid>
{
}