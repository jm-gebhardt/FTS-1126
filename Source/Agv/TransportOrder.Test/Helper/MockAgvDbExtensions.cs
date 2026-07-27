using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class MockAgvDbExtensions
{
    /// <summary>
    /// Extension block for <see cref="Mock{IAgvDb}"/>.
    /// </summary>
    extension(Mock<IAgvDb> repoMock)
    {
        /// <summary>
        /// Adds a list of <see cref="Agv"/>s to the mock repository.
        /// </summary>
        /// <param name="agvs">The list of <see cref="Agv"/>s to add.</param>
        public void AddAgvs(List<Agv> agvs) => repoMock.AddAgvsCore([.. agvs]);

        /// <summary>
        /// Adds a single <see cref="Agv"/> to the mock repository.
        /// </summary>
        /// <param name="agv">The <see cref="Agv"/> to add.</param>
        public void AddAgvs(Agv agv) => repoMock.AddAgvsCore([agv]);

        /// <summary>
        /// Adds multiple <see cref="Agv"/>s to the mock repository.
        /// </summary>
        /// <param name="agvs">The <see cref="Agv"/>s to add.</param>
        public void AddAgvs(params Agv[] agvs) => repoMock.AddAgvsCore(agvs);

        private void AddAgvsCore(Agv[] agvs)
        {
            repoMock.Setup(s => s.Agvs.GetAllAgvsAsync()).ReturnsAsync(agvs);

            repoMock.Setup(r => r.Agvs.GetAgvAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid c, CancellationToken _) =>
            {
                try
                {
                    return agvs.First(a => a.CorrelationId == c);
                }
                catch (InvalidOperationException)
                {
                    throw new AgvNotFoundException(c);
                }
            });
            repoMock.Setup(r => r.Agvs.TryGetAgvAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Guid c, CancellationToken _) => { return agvs.FirstOrDefault(a => a.CorrelationId == c); });

            repoMock.Setup(r => r.Agvs.Count).Returns(agvs.Count);
            repoMock.Setup(r => r.Agvs.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => agvs.Any(a => a.CorrelationId == c));
            repoMock.Setup(r => r.Agvs.GetAllAgvsWithGraphAsync()).ReturnsAsync(agvs.Where(a => a.GraphExists).ToList());
            repoMock.Setup(r => r.Agvs.GetChargingAgvsWithMinBatteryLevelAsync()).ReturnsAsync(agvs);
            repoMock.Setup(r => r.Agvs.GetIdleAgvsAsync()).ReturnsAsync(agvs.Where(a => a.CurrentAgvState == AgvState.Idle).ToList());
            repoMock.Setup(r => r.Agvs.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((List<Guid> cs) => agvs.Where(a => cs.Count == 0 || cs.Contains(a.CorrelationId)).ToList());
            repoMock.AddNonExistingAgvs(agvs.Select(a => a.CorrelationId).ToArray());
            repoMock.Setup(r => r.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(EfAgvSettingsProvider.DefaultSettings);
            repoMock.Setup(r => r.AgvSettings.GetSettingsGroupForAgv(It.IsAny<Agv?>())).ReturnsAsync(new AgvSettingsGroup(GroupType.All));
            repoMock.Setup(r => r.Agvs.GetReadyAgvsWithAtLeastLowBatteryLevel()).ReturnsAsync(agvs);
            repoMock.Setup(r => r.Agvs.SelectInfinitelyChargingAgvsWithAboveMinChargeUpBatteryLevel(It.IsAny<ICollection<Guid>>())).ReturnsAsync(agvs);
            repoMock.Setup(r => r.Agvs.SelectAgvsWithAboveCriticalBatteryLevel(It.IsAny<ICollection<Guid>>())).ReturnsAsync(agvs);
            repoMock.Setup(r => r.Agvs.HasOnlineAgvs()).ReturnsAsync(true);
            repoMock.Setup(r => r.Agvs.GetNotChargingAgvsWithCriticalBatteryLevelThatCanReachCharger(It.IsAny<Guid?>())).ReturnsAsync(agvs);
            repoMock.Setup(r => r.Agvs.GetAnyOnlineAgv()).ReturnsAsync(agvs.FirstOrDefault(a => a.IsOnline));
            repoMock.Setup(x => x.Positions.GetPosition(It.IsAny<Guid>())).Returns((Guid c) =>
            {
                var agv = agvs.Single(a => a.CorrelationId == c);
                return new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0));
            });
            repoMock.Setup(x => x.Positions.GetPositionIfExisting(It.IsAny<Guid>())).Returns((Guid c) =>
            {
                var agv = agvs.SingleOrDefault(a => a.CorrelationId == c);
                return agv == null ? null : new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0));
            });
            repoMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync([]);

            foreach (var agv in agvs)
            {
                Guid? lastNode = agv.AgvStatus?.LastNodeId;
                if (lastNode != null)
                {
                    repoMock.AddNode(lastNode.Value.ToGraphNode());
                }
            }
        }

        private void AddNonExistingAgvs(params Guid[] agvCorrelationIds)
        {
            repoMock.Setup(r => r.Agvs.ThrowIfAgvNotExistsAsync(It.IsAny<Guid>())).Callback((Guid correlationId) =>
            {
                if (agvCorrelationIds.All(a => a != correlationId))
                {
                    throw new AgvNotFoundException(correlationId);
                }
            });
            repoMock.Setup(r => r.Agvs.ThrowIfAgvsNotExistAsync(It.IsAny<ICollection<Guid>>())).Callback((ICollection<Guid> correlationIds) =>
            {
                List<Guid> notExistingAgvs = correlationIds.Except(agvCorrelationIds).ToList();
                if (notExistingAgvs.Count != 0)
                {
                    throw new AgvsNotFoundException(notExistingAgvs);
                }
            });
        }

        /// <summary>
        /// Adds <see cref="Zone"/>s to the mock repository.
        /// </summary>
        /// <param name="zones">The <see cref="Zone"/>s to add.</param>
        public void AddZones(ICollection<Zone> zones)
        {
            repoMock.Setup(r => r.Zones.GetZones(It.IsAny<string[]>())).ReturnsAsync(zones);
            repoMock.Setup(r => r.Zones.GetZones()).ReturnsAsync(zones);
        }

        /// <summary>
        /// Adds an <see cref="AgvPositionStatus"/> to the mock repository.
        /// </summary>
        /// <param name="position">The <see cref="AgvPositionStatus"/> to add.</param>
        public void AddPosition(AgvPositionStatus position)
        {
            repoMock.Setup(x => x.Positions.GetPosition(It.IsAny<Guid>())).Returns(position);
        }

        /// <summary>
        /// Adds an <see cref="AgvGraph"/> to the mock repository.
        /// </summary>
        /// <param name="agvGraph">The <see cref="AgvGraph"/> to add.</param>
        /// <param name="graphExists">Whether the graph exists.</param>
        public void AddAgvGraph(AgvGraph? agvGraph = null, bool graphExists = true)
        {
            repoMock.Setup(x => x.Graphs.GetAgvGraphAsync(It.IsAny<Guid>()))!.ReturnsAsync(agvGraph);
            repoMock.Setup(x => x.Graphs.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(graphExists);
            repoMock.Setup(x => x.Graphs.ExistsAsync()).ReturnsAsync(graphExists);
        }

        /// <summary>
        /// Sets up fallback behavior for node retrieval methods to throw <see cref="NodeNotFoundException"/>.
        /// </summary>
        public void SetupNodeFallbacks()
        {
            repoMock.Setup(x => x.Graphs.GetNodeAsync(It.IsAny<Guid>())).Returns<Guid>(id => throw new NodeNotFoundException(id));
            repoMock.Setup(x => x.Graphs.TryGetNodeByIdAsync(It.IsAny<Guid>())).Returns<Guid>(id => throw new NodeNotFoundException(id));
            repoMock.Setup(x => x.Graphs.GetNodeAsync(It.IsAny<Guid>())).Returns<Guid>(id => throw new NodeNotFoundException(id));
            repoMock.Setup(x => x.Graphs.TryGetNodeByIdAsync(It.IsAny<Guid>())).Returns<Guid>(id => throw new NodeNotFoundException(id));
        }

        /// <summary>
        /// Adds a single <see cref="GraphNode"/> to the mock repository.
        /// </summary>
        /// <param name="node">The <see cref="GraphNode"/> to add.</param>
        public void AddNode(GraphNode? node) => repoMock.AddNode([node]);

        /// <summary>
        /// Adds multiple <see cref="GraphNode"/>s to the mock repository.
        /// </summary>
        /// <param name="nodes">The <see cref="GraphNode"/>s to add.</param>
        public void AddNode(params GraphNode?[] nodes)
        {
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    repoMock.Setup(x => x.Graphs.GetNodeAsync(node.Id)).ReturnsAsync(node);
                    repoMock.Setup(x => x.Graphs.TryGetNodeByIdAsync(node.Id)).ReturnsAsync(node);
                    repoMock.Setup(x => x.Graphs.GetNodeAsync(node.Id)).ReturnsAsync(node);
                    repoMock.Setup(x => x.Graphs.TryGetNodeByIdAsync(node.Id)).ReturnsAsync(node);
                }
            }
        }

        /// <summary>
        /// Adds <see cref="AgvData"/> to the mock repository.
        /// </summary>
        /// <param name="agvData">The <see cref="AgvData"/> to add.</param>
        public void AddAgvData(AgvData? agvData)
        {
            repoMock.Setup(x => x.Data.GetSelectedAgvDataAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync([agvData!]);
            repoMock.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync(agvData);
            repoMock.Setup(x => x.Data.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        }

        /// <summary>
        /// Adds <see cref="AgvData"/> to the mock repository using a factory function.
        /// </summary>
        /// <param name="agvData">The factory function to create <see cref="AgvData"/>.</param>
        public void AddAgvData(Func<Guid, AgvData?> agvData)
        {
            repoMock.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync(agvData);
            repoMock.Setup(x => x.Data.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        }

        /// <summary>
        /// Adds active <see cref="AgvError"/>s to the mock repository.
        /// </summary>
        /// <param name="errors">The <see cref="AgvError"/>s to add.</param>
        public void AddActiveErrors(IList<AgvError> errors)
        {
            repoMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(errors.AsReadOnly);
            repoMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<IList<Guid>>())).ReturnsAsync(errors.AsReadOnly);
        }

        /// <summary>
        /// Gets an <see cref="Agv"/> by correlation ID from the mock repository.
        /// </summary>
        /// <param name="agvCorrelationId">The correlation ID of the <see cref="Agv"/> to retrieve.</param>
        /// <returns>The <see cref="Agv"/> with the specified correlation ID.</returns>
        public Task<Agv> GetAgvAsync(Guid agvCorrelationId)
        {
            return repoMock.Object.Agvs.GetAgvAsync(agvCorrelationId);
        }
    }
}