using System;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;

namespace Mini_Services.api.Entities
{
    public class Hangman
    {
        public Guid sessionId {get; init;}
        public string word {get; init;}
        public char[] wordProgress {get; init;}
        public char[] guessedLetters {get; init;}
        public int wrongGuesses {get; init;}
        public string picture {get; init;}
    }
}