using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// The in-memory cache for the status of order actions.
/// </summary>
public class OrderActionStatusCache : ConcurrentDictionaryProvider<Guid, IList<OrderActionState>>, IOrderActionStatusCacheProvider;