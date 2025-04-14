using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IPrismatCommunicationStationRepository"/>>
public class PrismatCommunicationStationRepository(PrismatCommunicationDbContext context): PrismatCommunication.Persistence.Repositories.EfStationRepository(context), IPrismatCommunicationStationRepository;