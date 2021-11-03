using System;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;

namespace Mini_Services.api.Entities
{
    public record Player
    {
        public Guid Id {get; init;}
        public string username {get; init;}
        public int wins {get; init;}
        public int losses {get; init;}
        public int draws {get; init;}
        public int score {get; init;}
    }
}