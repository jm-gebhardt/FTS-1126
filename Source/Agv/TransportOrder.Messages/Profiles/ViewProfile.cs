using AutoMapper;
using JetBrains.Annotations;
using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

namespace Gebhardt.Acc.TransportOrder.Messages.Profiles;

/// <summary>
/// Maps view commands to the view domain model.
/// </summary>
[UsedImplicitly]
public class ViewProfile : Profile
{
    /// <inheritdoc cref="ViewProfile"/>
    public ViewProfile()
    {
        CreateMap<AddView, View>()
            .ConvertUsing(src => new View(src.CorrelationId, src.Name, src.LevelId, src.IsDefault, src.Area));

        CreateMap<SetViewBackground, ViewBackground>();
    }
}