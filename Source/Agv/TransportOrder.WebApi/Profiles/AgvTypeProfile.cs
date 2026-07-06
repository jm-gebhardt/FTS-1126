using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvType;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Mapping for AGV types.
/// </summary>
[UsedImplicitly]
public class AgvTypeProfile : Profile
{
    /// <inheritdoc/>
    public AgvTypeProfile()
    {
        CreateMap<AgvType, AgvTypeDto>();
    }
}
