using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.Telegram;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Repositories;

/// <summary>
/// Provides a repository for telegram related queries.
/// </summary>
public interface ITelegramRepository: IRepository<Telegram>;