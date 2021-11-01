using Mini_Services.api.Dtos;
using Mini_Services.api.Entities;
using Mini_Services.Api.Dtos;
using Mini_Services.Api.Entities;

namespace Mini_Services.Api
{
    public static class Extensions
    {
        public static ItemDto AsDto(this Item item)
        {
            return new ItemDto
            {
                Id= item.Id, 
                Name = item.Name,
                Price = item.Price,
                CreatedDate = item.CreatedDate
            };
            
        }

        public static TicTacToeDto AsTicTacToeDto(this TicTacToe ticTacToe)
        {
            return new TicTacToeDto
            {
                Id= ticTacToe.Id, 
                board = ticTacToe.board,
            };
            
        }

        public static HangmanDto AsHangmanDto(this Hangman hangman)
        {
            return new HangmanDto
            {
                sessionId= hangman.sessionId, 
                word = hangman.word,
                wordProgress = hangman.wordProgress,
                guessedLetters = hangman.guessedLetters,
                wrongGuesses = hangman.wrongGuesses,
                picture = hangman.picture
            };
            
        }
    }
}