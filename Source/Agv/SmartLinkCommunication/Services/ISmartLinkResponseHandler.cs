using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunication.Services;

/// <summary>
/// Handles the SmartLink data
/// </summary>
public interface ISmartLinkResponseHandler
{
    /// <summary>
    /// Processes the data
    /// </summary>
    /// <param name="data">The data</param>
    /// <returns></returns>
    Task HandleAsync(SmartLinkData data);
}