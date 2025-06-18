using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System.Monitoring;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.System.Monitoring;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

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
        
        CreateMap<CertificateInfo, CertificateExpirationDto>();
    }
}