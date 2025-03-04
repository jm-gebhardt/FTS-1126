using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <inheritdoc />
public class AgvErrorDictionaryProvider : ConcurrentDictionaryProvider<Guid, Lazy<List<AgvError>>>;
