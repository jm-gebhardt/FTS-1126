using AutoMapper;
using JetBrains.Annotations;
using DomainRoutePlan = Gebhardt.Acc.TransportOrder.Domain.Models.Route.RoutePlan;
using MessageRoutePlan = Gebhardt.Acc.Common.Messages.ActionGroups.RoutePlan;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>AutoMapper profile for route plan conversion.</summary>
[UsedImplicitly]
public class RoutePlanProfile : Profile
{
    /// <summary/>
    public RoutePlanProfile()
    {
        CreateMap<DomainRoutePlan, MessageRoutePlan>();
    }
}