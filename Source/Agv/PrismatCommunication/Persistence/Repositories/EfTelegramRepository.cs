using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.Telegram;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="ITelegramRepository"/>>
public class EfTelegramRepository(IDbContextFactory<PrismatCommunicationDbContext> contextFactory) : EfRepository<Telegram, PrismatCommunicationDbContext, IDbContextFactory<PrismatCommunicationDbContext>>(contextFactory), ITelegramRepository;
