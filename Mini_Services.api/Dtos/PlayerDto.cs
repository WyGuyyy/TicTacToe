using System;

namespace Mini_Services.api.Dtos
{
    public class PlayerDto
    {
        public Guid Id {get; init;}
        public string username {get; init;}
        public int wins {get; init;}
        public int losses {get; init;}
        public int draws {get; init;}
        public int score {get; init;}
    }
}