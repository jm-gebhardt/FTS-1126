using Gebhardt.Acc.Common;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IPrismatCommunicationShortLoopWeTargetStationRepository"/>>
[CanBeCaptiveDependency]
public class PrismatCommunicationShortLoopWeTargetStationRepository(IDbContextFactory<PrismatCommunicationDbContext> contextFactory): EfShortLoopWeTargetStationRepository(contextFactory), IPrismatCommunicationShortLoopWeTargetStationRepository;
