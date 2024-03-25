using Gebhardt.StoreWare.Agv.Common.Messages;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services.Filters;

public class Message : IMessageCreatedDate
{
    public Guid Key { get; } = Guid.NewGuid();

    public DateTime MessageCreatedDate { get; }

    public Message()
    {
    }

    public Message(DateTime messageCreatedDate)
    {
        MessageCreatedDate = messageCreatedDate;
    }
}