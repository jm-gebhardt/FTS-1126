using AutoMapper;
using Gebhardt.Acc.Common.Messages;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

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