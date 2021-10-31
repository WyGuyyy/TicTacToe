using Microsoft.AspNetCore.Mvc;
using Mini_Services.Api.Repositories;
using System.Collections.Generic;
using Mini_Services.Api.Entities;
using System;
using System.Linq;
using Mini_Services.Api.Dtos;
using Microsoft.AspNetCore.DataProtection;
using System.Data.Common;
using System.Threading.Tasks;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mini_Services.api.Repositories;
using Mini_Services.api.Dtos;
using Mini_Services.api.Entities;
using System.Runtime.CompilerServices;

namespace Mini_Services.Api.Controllers
{

    [ApiController]
    [Route("tictactoe")]
    public class TicTacToeController : ControllerBase
    {
        private readonly ITicTacToeRepository repository;  
        private readonly ILogger<TicTacToeController> logger;
        private readonly Random rand = new();

        public TicTacToeController(ITicTacToeRepository repository, ILogger<TicTacToeController> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        // GET /items
        [HttpPost("{symbol}/{difficulty}")]
        public async Task<ActionResult<string>> CreateSessionAsync(string symbol, string difficulty)
        {
            bool isNumeric = Int32.TryParse(difficulty, out int diff);
            string error = "";

            if(!isNumeric){
                error = "Difficulty must be a numeric entry. Session not created.";
            }

            if(diff < 1 || diff > 3){
                error = "Difficulty must be in the range 1-3. Session not created.";
            }

            if(!symbol.Equals("x", StringComparison.OrdinalIgnoreCase) && !symbol.Equals("o", StringComparison.OrdinalIgnoreCase)){
                error = "Symbol entry must be 'x' (or 'X') or 'o' (or 'O'). Session not created.";
            }

            if(!string.IsNullOrEmpty(error)){
                return BadRequest(error);
            }

            TicTacToe ticTacToe = new(){
                sessionId = Guid.NewGuid(),
                board = determineInitialBoard(symbol, diff),
                playerSymbol = Char.Parse(symbol),
                difficulty = diff
            };

            await repository.CreateSessionAsync(ticTacToe);

            string info = buildSessionOpenMessage(ticTacToe);

            return info;
        }

        [HttpPut]
        public async Task<ActionResult<string>> UpdateSessionAsync(UpdateTicTacToeDto updateTicTacToeDto)
        {
            bool isRowNumeric = Int32.TryParse(updateTicTacToeDto.row, out int row);
            bool isColumnNumeric = Int32.TryParse(updateTicTacToeDto.column, out int column);
            string error = "";

            if(!isRowNumeric || ! isColumnNumeric){
                error = "Row and column must be a numeric entry. Your move was canceled.";
            }

            if(row < 1 || row > 3 || column < 1 || column > 3){
                error = "Row and column must be in the range 1-3. Your move was canceled.";
            }

            TicTacToe openSession = await repository.GetSessionAsync(Guid.Parse(updateTicTacToeDto.sessionId));

            if(!(openSession.board[row][column] == ' ')){
                error = "The position you selected has already been filled. Your move was canceled. Please choose an empty position.";
            }

            if(!string.IsNullOrEmpty(error)){
                return BadRequest(error);
            }

            TicTacToe updatedSession = new(){
                sessionId = openSession.sessionId,
                board = playRound(openSession, row, column),
                playerSymbol = openSession.playerSymbol,
                difficulty = openSession.difficulty
            };

            await repository.UpdateSessionAsync(updatedSession);

            string info = buildSessionRoundMessage(updatedSession);

            return info;
        }

        public string buildSessionOpenMessage(TicTacToe ticTacToe){
            
            string strPlayerSymbol = (ticTacToe.playerSymbol == 'x' || ticTacToe.playerSymbol == 'X' ? "X (First Turn)" : "O (Second Turn)");
            string strCPUSymbol = (ticTacToe.playerSymbol == 'x' || ticTacToe.playerSymbol == 'X' ? "O (Second Turn)" : "X (First Turn)");
            string strDifficulty = (ticTacToe.difficulty == 1 ? "1 (Easy)" : (ticTacToe.difficulty == 2 ? "2 (Intermediate)" : "3 (Expert)"));
            string sessionId = ticTacToe.sessionId.ToString();
            char[][] currentBoard = ticTacToe.board;

            string board = $"   |   |   \n" +
                           $" {currentBoard[0][0]} | {currentBoard[0][1]} | {currentBoard[0][2]} \n" +
                           $"_ _|_ _|_ _\n" +
                           $"   |   |   \n" +
                           $" {currentBoard[1][0]} | {currentBoard[1][1]} | {currentBoard[1][2]} \n" +
                           $"_ _|_ _|_ _\n" +
                           $"   |   |   \n" +
                           $" {currentBoard[2][0]} | {currentBoard[2][1]} | {currentBoard[2][2]} \n" +
                           $"   |   |   \n".Replace("\n", Environment.NewLine);

            var message = $"Welcome to your new session of TicTacToe! Here are your session details:\n\n" + 
                          $"Player Symbol: {strPlayerSymbol}\n" +
                          $"CPU Symbol: {strCPUSymbol}\n" +
                          $"Difficulty: {strDifficulty}\n" +
                          $"Session ID: {sessionId}\n" +
                          $"Current Board:\n\n" +
                          $"{board}\n" +
                          "You can declare your next move by sending a PUT request with the JSON Object: {sessionId: <Session ID>, row: <Row #>, column: <Column #>}.".Replace("\n", Environment.NewLine);

            return message;

        }

        public char[][] determineInitialBoard(string symbol, int diff){

            char[][] board = new char[][] {new char[] {' ', ' ', ' '}, new char[] {' ', ' ', ' ',}, new char[] {' ', ' ', ' '}};

            if(symbol.Equals("x", StringComparison.OrdinalIgnoreCase)){
                return board;
            }

            if(diff == 1){
                int[][] moves = {new int[] {0, 1}, new int[] {1, 0}, new int[] {1, 2}, new int[] {2, 1}};
                int[] moveSet = moves[rand.Next(5)];
                board[moveSet[0]][moveSet[1]] = 'X';
            }else if(diff == 2){
                board[1][1] = 'X';
            }else{
                int[][] moves = {new int[] {0, 0}, new int[] {0, 2}, new int[] {2, 0}, new int[] {2, 2}};
                int[] moveSet = moves[rand.Next(5)];
                board[moveSet[0]][moveSet[1]] = 'X';
            }

            return board;

        }

        public char[][] playRound(TicTacToe openSession, int row, int column){
            
            char playerSymbol = openSession.playerSymbol;
            char computerSymbol = (playerSymbol == 'x' || playerSymbol == 'X' ? 'O' : 'X');
            int difficulty = openSession.difficulty;
            char[][] currentBoard = openSession.board;

            int numberOfPositionsFilled = countPositionsFilled(currentBoard);

            //Players turn
            currentBoard[row][column] = playerSymbol;
            numberOfPositionsFilled++;

            if(checkWinner(currentBoard)){
                currentBoard[0][0] = 'P';
                return currentBoard;
            }else if(numberOfPositionsFilled == 9){
                currentBoard[0][0] = 'D';
                return currentBoard;
            }

            //Computers turn
            int[] cpuMoves = decideCPUMove(currentBoard, difficulty);
            currentBoard[cpuMoves[0]][cpuMoves[1]] = computerSymbol;

            if(checkWinner(currentBoard)){
                currentBoard[0][0] = 'C';
                return currentBoard;
            }else if(numberOfPositionsFilled == 9){
                currentBoard[0][0] = 'D';
                return currentBoard;
            }

            return currentBoard;

        }

        //Brain of the computer player (CPU)
        public int[] decideCPUMove(char[][] board, int difficulty){
            int[] move = new int[] {0, 0};

            return move;
        }

        public bool checkWinner(char[][] board){
            bool isWinner = false;

            //implement this next time

            return isWinner;
        }

        public int countPositionsFilled(char[][] board){

            int fillCount = 0;

            for(int row = 0; row < board.Length; row++){
                for(int column = 0; column < board[row].Length; column++){
                    if(board[row][column] != ' '){
                        fillCount++;
                    }
                }
            }

            return fillCount;

        }

    }
}