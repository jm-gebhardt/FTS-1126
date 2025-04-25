using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IPrismatCommunicationSystemStatusRepository"/>>
public class PrismatCommunicationSystemStatusRepository(PrismatCommunicationDbContext context) : PrismatCommunication.Persistence.Repositories.EfSystemStatusRepository(context), IPrismatCommunicationSystemStatusRepository;
