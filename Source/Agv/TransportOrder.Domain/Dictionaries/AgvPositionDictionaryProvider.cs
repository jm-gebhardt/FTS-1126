using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <inheritdoc />
public class AgvPositionDictionaryProvider : ConcurrentDictionaryProvider<Guid, AgvPositionStatus>;