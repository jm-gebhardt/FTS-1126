using Gebhardt.Acc.SmartLinkCommunication.Models;

namespace Gebhardt.Acc.SmartLinkCommunication.Services;

/// <summary>
/// Interface for getting data from SmartLink
/// </summary>
public interface ISmartLinkService
{
    /// <summary>
    /// Get data from SmartLink module
    /// </summary>
    /// <returns></returns>
    Task<SmartLinkData> GetDataAsync();
}