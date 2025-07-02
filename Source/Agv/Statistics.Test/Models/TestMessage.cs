using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.Contracts;

namespace Gebhardt.Acc.Statistics.Test.Models;

public class TestMessage : IAgvSerialNumber, IWithAgvPosition, IMessageCreatedDate
{
    public string SerialNumber { get; init; } = "AGV123";

    public AgvPosition AgvPosition { get; init; } = new(true, 1, 2, 0, 0);

    public DateTime MessageCreatedDate { get; } = DateTime.UtcNow;
}