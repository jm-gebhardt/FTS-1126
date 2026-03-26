using Gebhardt.Acc.TestPlanRunner.Domain.Exceptions;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.Messages.Requests;
using Gebhardt.Acc.TestPlanRunner.Messages.Responses;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TestPlanRunner.Services.RequestResponseHandlers;

/// <summary>
/// Handler for getting test plans.
/// </summary>
[UsedImplicitly]
public class GetTestPlansHandler :
    IConsumer<GetTestPlans>,
    IConsumer<GetTestPlan>
{
    private readonly ITestPlanManager _testPlanManager;

    /// <inheritdoc cref="GetTestPlansHandler"/>
    public GetTestPlansHandler(ITestPlanManager testPlanManager)
    {
        _testPlanManager = testPlanManager;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetTestPlans> context)
    {
        IEnumerable<TestPlan> testPlans = _testPlanManager.GetActiveTestPlans();
        await context.RespondAsync(new GotTestPlans(testPlans.ToList()));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetTestPlan> context)
    {
        var testPlan = _testPlanManager.GetTestPlan(context.Message.TestPlanName);
        if (testPlan == null)
        {
            throw new TestPlanNotFoundException(context.Message.TestPlanName);
        }
        await context.RespondAsync(new GotTestPlan(testPlan));
    }
}