using System;

namespace Mini_Services.api.Entities
{
    public record TicTacToe
    {
        public Guid Id {get; init;}
        public char[][] board {get; init;}
        public char playerSymbol {get; init;}
        public int difficulty {get; set;}
    }
}