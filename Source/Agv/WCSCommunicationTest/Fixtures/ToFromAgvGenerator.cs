using Gebhardt.Acc.WCSCommunication.WcsApi.DbInterface.Models;

namespace Gebhardt.Acc.WCSCommunicationTest.Fixtures;

/// <summary>
/// Generate ToFromAgv test entries
/// </summary>
public static class ToFromAgvGenerator
{
    /// <summary>
    /// Generate Testing ToAgvEntry 
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="recordType"></param>
    /// <param name="messageStatus"></param>
    /// <param name="leNo"></param>
    /// <returns></returns>
    public static ToAgv GenerateToAgvEntry(
        string? orderId = null,
        string? source = null,
        string? destination = null,
        RecordType? recordType = null,
        MessageStatus? messageStatus = null,
        string? leNo = null
    )
    {
        return new ToAgv
        {
            OrderId = orderId ?? Guid.NewGuid().ToString(),
            Source = source ?? Guid.NewGuid().ToString(),
            Destination = destination ?? Guid.NewGuid().ToString(),
            RecordType = recordType ?? RecordType.TransportOrder,
            MessageStatus = messageStatus ?? MessageStatus.Pending,
            LeNo = leNo ?? Guid.NewGuid().ToString()
        };
    }
}