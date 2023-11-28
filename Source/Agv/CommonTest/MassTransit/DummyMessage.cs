using MassTransit;

namespace Gebhardt.StoreWare.Agv.CommonTest.MassTransit;

public record DummyMessage(Guid CorrelationId) : CorrelatedBy<Guid>;