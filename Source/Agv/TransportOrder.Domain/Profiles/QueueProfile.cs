using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Profiles;

/// <summary>
/// Profile for Queue.
/// </summary>
[UsedImplicitly]
public class QueueProfile : Profile
{
    /// <inheritdoc />
    public QueueProfile()
    {
        CreateMap<QueueNode, Common.Messages.Queue.QueueNode>()
            .ConvertUsing((src, _) =>
                new Common.Messages.Queue.QueueNode(
                    new BaseNode(src.Node!.Id, src.Node.Name),
                    src.Position,
                    src.ReservedAgv,
                    src.OccupiedAgv));

        CreateMap<Queue, Common.Messages.Queue.Queue>()
            .ForMember(dest => dest.CorrelationId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.QueueNodes, opt => opt.MapFrom(src => src.Nodes))
            .ConstructUsing((src, ctx) =>
                new Common.Messages.Queue.Queue(src.Id, src.Name, ctx.Mapper.Map<List<Common.Messages.Queue.QueueNode>>(src.Nodes)));
    }
}