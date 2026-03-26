using AutoMapper;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.Messages.Requests;
using Gebhardt.Acc.TestPlanRunner.Messages.Responses;
using Gebhardt.Acc.TestPlanRunner.WebApi.Dtos;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Gebhardt.Acc.TestPlanRunner.WebApi.Controllers;

/// <summary>
/// Controller for managing test plans.
/// </summary>
[ApiController]
public class TestPlanController : ControllerBase
{
    private readonly IRequestClient<GetTestPlans> _getTestPlansClient;
    private readonly IRequestClient<GetTestPlan> _getTestPlanClient;
    private readonly IRequestClient<SetTestPlan> _setTestPlanClient;
    private readonly IRequestClient<DeleteTestPlan> _deleteTestPlanClient;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="TestPlanController"/>
    public TestPlanController(
        IRequestClient<GetTestPlans> getTestPlansClient,
        IRequestClient<GetTestPlan> getTestPlanClient,
        IRequestClient<SetTestPlan> setTestPlanClient,
        IRequestClient<DeleteTestPlan> deleteTestPlanClient,
        IMapper mapper)
    {
        _getTestPlansClient = getTestPlansClient;
        _getTestPlanClient = getTestPlanClient;
        _setTestPlanClient = setTestPlanClient;
        _deleteTestPlanClient = deleteTestPlanClient;
        _mapper = mapper;
    }

    /// <summary>
    /// Gets all active test plans.
    /// </summary>
    /// <response code="200">Ok</response>
    [HttpGet]
    [Route("/testPlans")]
    public async Task<ActionResult<IEnumerable<TestPlanDto>>> GetTestPlans()
    {
        Response<GotTestPlans> response = await _getTestPlansClient.GetResponse<GotTestPlans>(new GetTestPlans());
        var dto = _mapper.Map<IEnumerable<TestPlanDto>>(response.Message.TestPlans);
        return Ok(dto);
    }

    /// <summary>
    /// Gets a specific test plan by name.
    /// </summary>
    /// <response code="200">Ok</response>
    /// <response code="404">Not Found</response>
    [HttpGet]
    [Route("/testPlan/{name}")]
    public async Task<ActionResult<TestPlanDto>> GetTestPlan(string name)
    {
        Response<GotTestPlan> response = await _getTestPlanClient.GetResponse<GotTestPlan>(new GetTestPlan(name));
        var dto = _mapper.Map<TestPlanDto>(response.Message.TestPlan);
        return Ok(dto);
    }

    /// <summary>
    /// Sets (creates or updates) a test plan.
    /// </summary>
    /// <response code="204">Ok</response>
    /// <response code="400">Bad Request</response>
    [HttpPost]
    [Route("/testPlan/{name}")]
    public async Task<ActionResult> SetTestPlan(string name, [FromBody] TestPlanDto testPlanDto)
    {
        var testPlan = _mapper.Map<TestPlan>(testPlanDto) with { Name = name };
        await _setTestPlanClient.GetResponse<TestPlanSet>(new SetTestPlan(testPlan));
        return NoContent();
    }

    /// <summary>
    /// Deletes a test plan.
    /// </summary>
    /// <response code="204">Ok</response>
    /// <response code="404">Not Found</response>
    [HttpDelete]
    [Route("/testPlan/{name}")]
    public async Task<ActionResult> DeleteTestPlan(string name)
    {
        await _deleteTestPlanClient.GetResponse<TestPlanDeleted>(new DeleteTestPlan(name));
        return NoContent();
    }
}