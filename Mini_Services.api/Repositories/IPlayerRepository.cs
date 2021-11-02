using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mini_Services.api.Dtos;
using Mini_Services.api.Entities;

namespace Mini_Services.api.Repositories
{
    public interface IPlayerRepository
    {
        Task CreateAccountAsync(Player player);
        Task<Player> GetAccountAsync(Guid Id);
        Task<IEnumerable<Player>> GetAccountsAsync();
        Task UpdatePlayerAsync(Player player);
        Task DeletePlayerAsync(Guid Id);
    }
}