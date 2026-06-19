using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Messages.ActionGroups;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <summary>
/// The in-memory cache holding the latest dispatched route plan per agv.
/// </summary>
public class AgvRoutePlanCache : ConcurrentDictionaryProvider<RoutePlan>, IAgvRoutePlanCacheProvider;