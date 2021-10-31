using System;

namespace Mini_Services.api.Dtos
{
    public class TicTacToeDto
    {
        public Guid sessionId {get; init;}
        public char[][] board {get; init;}
    }
}