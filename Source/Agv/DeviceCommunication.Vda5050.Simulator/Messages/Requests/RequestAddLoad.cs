using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;

/// <summary>
/// Provides a request to add a load to an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="LoadId">The load id.</param>
/// <param name="LoadType">The type of the load.</param>
/// <param name="LoadPosition">Indicates which load handling/carrying unit of the AGV is used. Optional for vehicles with only one loadPosition.</param>
public record RequestAddLoad(Guid CorrelationId, string LoadId, string? LoadType, string? LoadPosition) : CorrelatedBy<Guid>;