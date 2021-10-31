using System;

namespace Mini_Services.api.Dtos
{
    public class UpdateTicTacToeDto
    {
        public string sessionId {get; init;}
        public string row {get; init;}
        public string column {get; init;}
    }
}