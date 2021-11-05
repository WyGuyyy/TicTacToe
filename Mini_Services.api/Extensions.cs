using Mini_Services.api.Dtos;
using Mini_Services.api.Entities;

namespace Mini_Services.Api
{
    public static class Extensions
    {
        public static TicTacToeDto AsTicTacToeDto(this TicTacToe ticTacToe)
        {
            return new TicTacToeDto
            {
                Id= ticTacToe.Id, 
                board = ticTacToe.board,
            };
            
        }

        public static PlayerDto AsPlayerDto(this Player player)
        {
            return new PlayerDto
            {
                Id= player.Id, 
                username = player.username,
                wins = player.wins,
                losses = player.losses,
                score = player.score
            };
            
        }
    }
}