using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IPrismatCommunicationShortLoopWeTargetStationRepository"/>>
public class PrismatCommunicationShortLoopWeTargetStationRepository(PrismatCommunicationDbContext context): EfShortLoopWeTargetStationRepository(context), IPrismatCommunicationShortLoopWeTargetStationRepository;
