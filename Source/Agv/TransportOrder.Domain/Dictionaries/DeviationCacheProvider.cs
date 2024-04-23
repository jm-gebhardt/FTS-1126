using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <inheritdoc />
public class DeviationCacheProvider : ConcurrentDictionaryProvider<string, (double value, DateTime lastUpdated)>;