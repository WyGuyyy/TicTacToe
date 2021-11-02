using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mini_Services.api.Dtos;
using Mini_Services.api.Entities;
using Mini_Services.api.Repositories;
using Mini_Services.Api;

namespace Mini_Services.api.Controllers
{
    [ApiController]
    [Route("player")]
    public class PlayerController : ControllerBase
    {
        private readonly IPlayerRepository repository;
        private readonly ILogger<PlayerController> logger;

        public PlayerController(IPlayerRepository repository, ILogger<PlayerController> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<PlayerDto>> CreateAccountAsync(PlayerCreateDto playerCreateDto)
        {
            Player player = new()
            {
                Id = Guid.NewGuid(),
                username = playerCreateDto.username,
                wins = 0,
                losses = 0,
                score = 0
            };

            await repository.CreateAccountAsync(player);

            return CreatedAtAction(nameof(GetAccountAsync), new {id = player.Id}, player.AsPlayerDto());
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<PlayerDto>> GetAccountAsync(Guid Id)
        {
            var player = await repository.GetAccountAsync(Id);

            if(player is null){
                return NotFound();
            }

            return player.AsPlayerDto();
        }

        [HttpGet]
        public async Task<IEnumerable<Player>> GetAccountsAsync()
        {
            var players = await repository.GetAccountsAsync();

            return players;
        }

        [HttpPut("{Id}")]
        public async Task<ActionResult> UpdateAccountsAsync(Guid Id, PlayerUpdateDto playerUpdateDto)
        {
            var existingPlayer = await repository.GetAccountAsync(Id);

            if(existingPlayer is null){
                return NotFound();
            }

            Player updatedPlayer = existingPlayer with {
                username = playerUpdateDto.username
            };

            await repository.UpdatePlayerAsync(updatedPlayer);

            return NoContent();
        }

        [HttpDelete("{Id}")]
        public async Task<ActionResult> DeletePlayerAsync(Guid Id)
        {
            var existingPlayer = await repository.GetAccountAsync(Id);

            if(existingPlayer is null){
                return NotFound();
            }

            await repository.DeletePlayerAsync(Id);

            return NoContent();
        }

        [HttpGet]
        [Route("leaderboard")]
        public async Task<string> GetLeaderboardAsync()
        {
            string leaderboard = "";

            List<Player> players = (await repository.GetAccountsAsync()).ToList();

            leaderboard = buildLeaderboard(players);

            return leaderboard;
        }

        public string buildLeaderboard(List<Player> players)
        {
            string leaderboard = "";
            
            players.OrderByDescending(player => player.score);

            for(int count = 0; count < players.Count(); count++){

                double ratio = players.ElementAt(count).wins / (players.ElementAt(count).losses == 0 ? 1 : (players.ElementAt(count).losses * 1.0));

                leaderboard += $"Player: {players.ElementAt(count).username}\n" +
                               $"W/L Ratio: {ratio}\n" +
                               $"Score: {players.ElementAt(count).score}\n\n".Replace("\n", Environment.NewLine);
            }

            return leaderboard;
        }
    }
}