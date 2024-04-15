using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for WebApi project. Includes global ignore for <see cref="DatedMessage.MessageCreatedDate"/>
/// </summary>
public abstract class WebApiProfile : Profile
{
    /// <inheritdoc />
    protected WebApiProfile()
    {
        AddGlobalIgnore(nameof(DatedMessage.MessageCreatedDate));
    }
}