using Gebhardt.Acc.Common.Dictionaries;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <inheritdoc />
public class NodeTriggersDictionaryProvider : ConcurrentDictionaryProvider<string, List<Guid>>;