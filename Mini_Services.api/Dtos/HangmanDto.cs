using System;

namespace Mini_Services.api.Dtos
{
    public class HangmanDto
    {
        public Guid sessionId {get; init;}
        public string word {get; init;}
        public char[] wordProgress {get; init;}
        public char[] guessedLetters {get; init;}
        public int wrongGuesses {get; init;}
        public string picture {get; init;}
    }
}