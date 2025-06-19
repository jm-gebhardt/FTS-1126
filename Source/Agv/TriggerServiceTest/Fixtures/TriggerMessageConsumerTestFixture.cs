using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TriggerService.Domain.Services;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TriggerService.Test.Fixtures;

public class TriggerMessageConsumerTestFixture<TConsumer> : MassTransitConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
{
    private readonly Mock<ITriggerManager> _triggerManagerMock = new();
   
    protected override MassTransitConsumerTestFixture<TConsumer> AddServices(IServiceCollection collection)
    {
        base.AddServices(collection);
        collection.AddTransient(_ => _triggerManagerMock.Object);
        return this;
    }

    public TriggerMessageConsumerTestFixture()
    {
        Build();
        _triggerManagerMock.Setup(x => x.InvokeTrigger(It.IsAny<Guid>(), It.IsAny<Dictionary<string, ConfigurationParameter>>(),  It.IsAny<AgvData>(), It.IsAny<AgvStatus>(),
                It.IsAny<ActionGroup?>(), It.IsAny<ICollection<Cargo>>(), It.IsAny<ICollection<AgvError>>()))
            .Verifiable();
        _triggerManagerMock.Setup(x => x.UpdateDefaultConfigurationParameters(It.IsAny<Guid>(), It.IsAny<ICollection<ConfigurationParameter>>())).Verifiable();
    }

    public void VerifyTriggerInvoked(Guid triggerId, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
       _triggerManagerMock.Verify(x => x.InvokeTrigger(triggerId,
           It.IsAny<Dictionary<string, ConfigurationParameter>>(),
                It.Is<AgvData>(a => a.CorrelationId == agvData.CorrelationId), 
            It.Is<AgvStatus>(a => a.CorrelationId == agvStatus.CorrelationId),
            It.Is<ActionGroup?>( a => (a == null && actionGroup == null)|| a!.CorrelationId == actionGroup!.CorrelationId), 
            It.Is<ICollection<Cargo>>(c => !cargo.Select( ca => ca.CorrelationId).Except( c.Select( ca => ca.CorrelationId)).Any()), 
            It.Is<ICollection<AgvError>>( e => !errors.Select(er => er.CorrelationId).Except(e.Select(er =>er.CorrelationId)).Any())), 
            Times.Once);
    }

    public void VerifyLeaveTriggerInvoked(Guid triggerId, AgvData agvData, AgvStatus agvStatus, ActionGroup? actionGroup, ICollection<Cargo> cargo, ICollection<AgvError> errors)
    {
        _triggerManagerMock.Verify(x => x.LeaveTrigger(triggerId,
                It.IsAny<Dictionary<string, ConfigurationParameter>>(),
                It.Is<AgvData>(a => a.CorrelationId == agvData.CorrelationId),
                It.Is<AgvStatus>(a => a.CorrelationId == agvStatus.CorrelationId),
                It.Is<ActionGroup?>(a => (a == null && actionGroup == null)|| a!.CorrelationId == actionGroup!.CorrelationId),
                It.Is<ICollection<Cargo>>(c => !cargo.Select(ca => ca.CorrelationId).Except(c.Select(ca => ca.CorrelationId)).Any()),
                It.Is<ICollection<AgvError>>(e => !errors.Select(er => er.CorrelationId).Except(e.Select(er => er.CorrelationId)).Any())),
            Times.Once);
    }

    public void VerifyConfigurationParametersUpdated(Guid triggerId, ICollection<ConfigurationParameter> parameters)
    {
        _triggerManagerMock.Verify(x => x.UpdateDefaultConfigurationParameters(triggerId, It.Is<ICollection<ConfigurationParameter>>( c=> c.All(p => parameters.Any(a => a.Value == p.Value && a.Name == p.Name && a.Validation == p.Validation)))), Times.Once());
    }
}