using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Dictionaries;

/// <inheritdoc />
public class AgvPositionDictionaryProvider : ConcurrentDictionaryProvider<Guid, AgvPositionStatus>;