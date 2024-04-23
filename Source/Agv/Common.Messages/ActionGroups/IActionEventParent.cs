using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups;

/// <summary>
/// Used to identify that an event belongs to a parent object
/// </summary>
public interface IActionEventParent : CorrelatedBy<Guid>;