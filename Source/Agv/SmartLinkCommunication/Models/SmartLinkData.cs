namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

/// <summary>
/// Data of all IOs from SmartLink module
/// </summary>
public class SmartLinkData : Dictionary<string, PinStatus>
{
    /// <inheritdoc cref="SmartLinkData"/>
    /// <param name="data"></param>
    public SmartLinkData(IDictionary<string, PinStatus> data) : base(data)
    {
    }

    /// <summary>
    /// Deserialization constructor
    /// </summary>
    public SmartLinkData()
    {
    }
}