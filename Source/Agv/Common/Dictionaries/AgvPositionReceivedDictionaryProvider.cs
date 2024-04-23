using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;

namespace Gebhardt.Acc.Common.Dictionaries;

/// <summary>
/// A dictionary provider for <see cref="AgvPositionReceived"/>.
/// </summary>
public class AgvPositionReceivedDictionaryProvider : ConcurrentDictionaryProvider<IMessageFilter<AgvPositionReceived>>;