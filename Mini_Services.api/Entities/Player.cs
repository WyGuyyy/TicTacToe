using System;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;

namespace Mini_Services.api.Entities
{
    public record Player
    {
        public Guid id {get; init;}
        public string username {get; init;}
        public StatSet hangmanStats {get; init;}
        public StatSet ticTacToeStats {get; init;}
    }
}