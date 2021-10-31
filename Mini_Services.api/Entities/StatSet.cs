using System;
using MongoDB.Driver.Core.Operations;

namespace Mini_Services.api.Entities
{
    public record StatSet
    {
        public Guid id {get; init;}
        public int totalGames {get; init;}
        public int wins {get; init;}
        public int losses {get; init;}
        public double winRatio {get; init;}
        public double lossRatio {get; init;}
        public string rank {get; init;}
    }
}