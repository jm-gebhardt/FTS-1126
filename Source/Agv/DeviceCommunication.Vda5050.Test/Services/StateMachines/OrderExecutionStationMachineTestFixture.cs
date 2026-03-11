using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Sagas;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.StateMachines;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.StateMachines;

public class OrderExecutionStationMachineTestFixture(TimeSpan? inactivityTimeout = null) : BaseStateMachineTestFixture<OrderExecutionStateMachine, OrderExecutionSaga>(inactivityTimeout: inactivityTimeout)
{
    protected override IEnumerable<Profile> GetProfiles() => [new OrderProfile(), new AgvActionProfile()];

    public new OrderExecutionStationMachineTestFixture Build()
    {
        AddServices(sc =>
        {
            sc.AddSingleton(typeof(ISequenceCounter<>), typeof(SequenceCounter<>));
            sc.AddSingleton<IOrderActionStatusCacheProvider, OrderActionStatusCache>();
        });
        base.Build();
        return this;
    }

    public OrderExecutionSaga GetSaga(Guid correlationId)
    {
        return SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;
    }
}