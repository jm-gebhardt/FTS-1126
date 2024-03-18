using Gebhardt.StoreWare.Agv.Common.WebApi;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Dtos;

public class Message(DateTime messageCreatedDate) : IMessageCreatedDate
{
    public DateTime MessageCreatedDate { get; } = messageCreatedDate;
}