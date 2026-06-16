using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.GraphAnalysis;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for bounding box DTOs.
/// </summary>
[UsedImplicitly]
public class BoundingBoxProfile : WebApiProfile
{
    /// <inheritdoc cref="BoundingBoxProfile"/>
    public BoundingBoxProfile()
    {
        CreateMap<ResourceBoundingBoxContent, ResourceBoundingBoxDto>();
        CreateMap<BoundingBoxConflictContent, BoundingBoxConflictDto>();
    }
}