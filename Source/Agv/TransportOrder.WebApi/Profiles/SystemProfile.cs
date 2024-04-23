using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.System;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for <see cref="System" />.
/// </summary>
[UsedImplicitly]
public class SystemProfile : WebApiProfile
{
    /// <inheritdoc cref="SystemProfile" />
    public SystemProfile()
    {
        CreateMap<GotSystemTime, string>()
            .ConvertUsing((src, _, _) => src.SystemTime.ToUniversalIso8601());
    }
}