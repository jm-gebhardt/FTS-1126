using Gebhardt.StoreWare.Agv.Common.Dictionaries;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;

/// <inheritdoc />
public class DeviationCacheProvider : ConcurrentDictionaryProvider<string, (double value, DateTime lastUpdated)>;