using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

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