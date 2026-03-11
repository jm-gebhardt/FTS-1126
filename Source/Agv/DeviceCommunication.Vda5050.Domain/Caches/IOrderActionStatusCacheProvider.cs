using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;

/// <summary>
/// Provides an order action status cache.
/// </summary>
public interface IOrderActionStatusCacheProvider : IConcurrentDictionaryProvider<Guid, IList<OrderActionState>>;
