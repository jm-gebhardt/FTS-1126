using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;

namespace Gebhardt.StoreWare.Agv.Common.Dictionaries;

/// <summary>
/// A dictionary provider for <see cref="AgvPositionReceived"/>.
/// </summary>
public class AgvPositionReceivedDictionaryProvider : ConcurrentDictionaryProvider<IMessageFilter<AgvPositionReceived>>;