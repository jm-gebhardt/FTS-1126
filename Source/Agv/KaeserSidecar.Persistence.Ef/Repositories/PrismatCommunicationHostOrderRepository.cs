using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IPrismatCommunicationHostOrderRepository"/>>
public class PrismatCommunicationHostOrderRepository(PrismatCommunicationDbContext context): EfHostOrderRepository(context), IPrismatCommunicationHostOrderRepository;