using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <summary>
/// Provides a cache for the latest dispatched route plan per agv.
/// </summary>
public interface IAgvRoutePlanCacheProvider : IConcurrentDictionaryProvider<RoutePlan>;