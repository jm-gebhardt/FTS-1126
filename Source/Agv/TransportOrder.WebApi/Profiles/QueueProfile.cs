using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles;

/// <summary>
/// Profile for mapping queues
/// </summary>
public class QueueProfile : WebApiProfile
{
    /// <inheritdoc cref="QueueProfile"/>>
    public QueueProfile()
    {
        CreateMap<QueueNode, QueueNodeDto>()
            .ConstructUsing(_ => new QueueNodeDto())
            .ForMember(d => d.NodeId, opt => opt.MapFrom(s => s.Node != null ? s.Node.Name : string.Empty))
            .ForSourceMember(s => s.Node, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.LastReservedDate, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.LastOccupiedDate, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.ReservedAgv, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.OccupiedAgv, opt => opt.DoNotValidate());
       
        CreateMap<QueueNodeDto, Common.Messages.Queue.QueueNode>()
            .ForCtorParam(nameof(Common.Messages.Queue.QueueNode.NodeId), opt => opt.MapFrom(s => s.NodeId.ToGuid()));
        CreateMap<List<QueueNodeDto>, List<Common.Messages.Queue.QueueNode>>().ConvertUsing((src, _, context) => { return src.Select((dto, _) => context.Mapper.Map<Common.Messages.Queue.QueueNode>(dto)).ToList(); });
        CreateMap<Queue, QueueDto>()
            .ForSourceMember(s => s.Head, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.AgvFillQuantity, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.Tail, opt => opt.DoNotValidate())
            .ForSourceMember(s => s.IsFull, opt => opt.DoNotValidate());
    }
}