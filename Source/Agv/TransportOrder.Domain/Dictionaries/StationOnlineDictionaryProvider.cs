using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <inheritdoc />
public class StationOnlineDictionaryProvider : ConcurrentDictionaryProvider<Guid, bool?>;