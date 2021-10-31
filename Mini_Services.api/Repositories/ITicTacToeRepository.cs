using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mini_Services.api.Entities;

namespace Mini_Services.api.Repositories
{
    public interface ITicTacToeRepository
    {
        
        Task CreateSessionAsync(TicTacToe ticTacToe);
        Task<TicTacToe> GetSessionAsync(Guid sessionId);
        Task<IEnumerable<TicTacToe>> GetSessionsAsync();

    }
}