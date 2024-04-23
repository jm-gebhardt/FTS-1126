using MassTransit;

namespace Gebhardt.Acc.CommonTest.MassTransit;

public record DummyMessage(Guid CorrelationId) : CorrelatedBy<Guid>;