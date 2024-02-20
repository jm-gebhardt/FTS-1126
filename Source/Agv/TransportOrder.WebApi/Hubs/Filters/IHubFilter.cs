using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// A filter that can be used to determine if a message should be published.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IHubFilter<in T> where T : IMessageCreatedDate
{
    /// <summary>
    /// Determines if the message should be published.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>True if the dto should be published.</returns>
    bool ShouldPublish(T dto);
}