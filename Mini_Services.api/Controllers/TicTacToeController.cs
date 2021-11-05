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
using System.Collections;

namespace Mini_Services.Api.Controllers
{

    [ApiController]
    [Route("tictactoe")]
    public class TicTacToeController : ControllerBase
    {
        private readonly ITicTacToeRepository repository;  
        private readonly IPlayerRepository playerRepository;
        private readonly ILogger<TicTacToeController> logger;
        private readonly Random rand = new();
        
        //Lookup table for maximizer and minimizer
        Dictionary<char, int> lookupTable = new Dictionary<char, int>()
        {
            {'X', 1},
            {'O', -1}
        };

        public TicTacToeController(ITicTacToeRepository repository, IPlayerRepository playerRepository, ILogger<TicTacToeController> logger)
        {
            this.repository = repository;
            this.playerRepository = playerRepository;
            this.logger = logger;
        }

        
        [HttpPost("{symbol}/{difficulty}/{pId}")]
        public async Task<ActionResult<string>> CreateSessionAsync(string symbol, string difficulty, Guid pId)
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
                Id = Guid.NewGuid(),
                board = determineInitialBoard(symbol, diff),
                playerSymbol = Char.Parse(symbol.ToUpper()),
                difficulty = diff,
                playerId = pId
            };

            await repository.CreateSessionAsync(ticTacToe);

            string info = buildSessionOpenMessage(ticTacToe);

            return info;
        }

        [HttpGet]
        public async Task<IEnumerable<TicTacToe>> GetSessionsAsync(){

            var sessions = await repository.GetSessionsAsync();

            return sessions;

        }

        [HttpGet("{sessionId}")]
        public async Task<ActionResult<TicTacToe>> GetSessionAsync(Guid sessionId){

            var session = await repository.GetSessionAsync(sessionId);

            if(session is null){
                return NotFound("Sorry, the game session you specified does not exist in the system or was ended.");
            }

            return session;

        }

        [HttpDelete("{sessionId}")]
        public async Task<string> DeleteSessionAsync(Guid sessionId){
            var existingSession = await repository.GetSessionAsync(sessionId);

            if(existingSession is null){
                return "Sorry, the game session you specified for deletion does not exist in the system or was ended.";
            }

            await repository.DeleteSessionAsync(sessionId);

            return "Deletion successful!";
        }

        [HttpGet]
        [Route("help")]
        public ActionResult<string> getHelp()
        {
            string message = "The following are endpoints that can be called:\n\n" +
                             "(GET) /tictactoe/{sessionId} - Will return the details of a tic tac toe session identified by {sessionId}\n" +
                             "(GET) /tictactoe - Will return a list of the details for all currently opened tic tac toe sessions\n" +
                             "(GET) /tictactoe/help - Will return a list of the endpoints that can be called for tic tac toe\n" +
                             "(POST) /tictactoe/{playerSymbol}/{difficulty} - Will create a new tic tac toe session and return the details of the newly created session, where {playerSymbol} can be specified as either x/X (first player) or o/O (second player) and {difficulty} can be 1 (easy), 2 (intermediate) or 3 (expert)\n" +
                             "(PUT) /tictactoe - Will perform the next move for the player as specified in the body/payload of the request as the following JSON object {sessionId: {sessionId}, row: {row #}, column: {column #}} (The tic tac toe board is a 3x3 grid, so row and column can be any value 1-3)\n" +
                             "(DELETE) /tictactoe/{sessionId} - Will delete a tic tac toe session as identified by {sessionId}".Replace("\n", Environment.NewLine);

            return message;
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

            row = row - 1;
            column = column - 1;

            TicTacToe openSession = await repository.GetSessionAsync(Guid.Parse(updateTicTacToeDto.sessionId));

            if(openSession is null){
                return "Sorry, the game you specified to play does not exist in the system.";
            }

            if(!(openSession.board[row][column] == ' ')){
                error = "The position you selected has already been filled. Your move was canceled. Please choose an empty position.";
            }

            if(!string.IsNullOrEmpty(error)){
                return BadRequest(error);
            }

            var results = playRound(openSession, row, column);

            TicTacToe updatedSession = openSession with {
                board = results.board
            };

            string info = "";
            char playerSymbol = updatedSession.playerSymbol;
            char computerSymbol = playerSymbol == 'X' ? 'O' : 'X';

            if(results.winner == lookupTable[playerSymbol]){
                await repository.DeleteSessionAsync(updatedSession.Id);

                Player player = await playerRepository.GetAccountAsync(updatedSession.playerId);

                player = player with {
                    wins = player.wins + 1,
                    score = (((player.wins * 3) - (player.losses * 2)) + (player.draws/2))
                };

                await playerRepository.UpdatePlayerAsync(player);

                info = buildGameEndMessage(updatedSession, "Winner, winner, chicken dinner!!! You won!");
            }else if(results.winner == lookupTable[computerSymbol]){
                await repository.DeleteSessionAsync(updatedSession.Id);

                Player player = await playerRepository.GetAccountAsync(updatedSession.playerId);

                player = player with {
                    losses = player.losses + 1,
                    score = (((player.wins * 3) - (player.losses * 2)) + (player.draws/2))
                };
                
                await playerRepository.UpdatePlayerAsync(player);

                info = buildGameEndMessage(updatedSession, "Sorry, you lost. It looks like the AI got the best of you this time...");
            }else if(results.winner == 0){
                await repository.DeleteSessionAsync(updatedSession.Id);

                Player player = await playerRepository.GetAccountAsync(updatedSession.playerId);

                player = player with {
                    draws = player.draws + 1,
                    score = (((player.wins * 3) - (player.losses * 2)) + (player.draws/2))
                };
                
                await playerRepository.UpdatePlayerAsync(player);

                info = buildGameEndMessage(updatedSession, "Its a draw! Whatta game!");
            }else{
                await repository.UpdateSessionAsync(updatedSession);
                info = buildSessionRoundMessage(updatedSession);
            }

            return info;
        }

        //Build the game end message when there has been a winner or a draw, showing the end game board configuration
        public string buildGameEndMessage(TicTacToe updatedSession, string resultMessage){

            string strPlayerSymbol = (updatedSession.playerSymbol == 'X' ? "X (First Turn)" : "O (Second Turn)");
            string strCPUSymbol = (updatedSession.playerSymbol == 'X' ? "O (Second Turn)" : "X (First Turn)");
            string strDifficulty = (updatedSession.difficulty == 1 ? "1 (Easy)" : (updatedSession.difficulty == 2 ? "2 (Intermediate)" : "3 (Expert)"));
            string sessionId = updatedSession.Id.ToString();
            char[][] currentBoard = updatedSession.board;

            string board = $"   |   |   \n" +
                           $" {currentBoard[0][0]} | {currentBoard[0][1]} | {currentBoard[0][2]} \n" +
                           $"_ _|_ _|_ _\n" +
                           $"   |   |   \n" +
                           $" {currentBoard[1][0]} | {currentBoard[1][1]} | {currentBoard[1][2]} \n" +
                           $"_ _|_ _|_ _\n" +
                           $"   |   |   \n" +
                           $" {currentBoard[2][0]} | {currentBoard[2][1]} | {currentBoard[2][2]} \n" +
                           $"   |   |   \n".Replace("\n", Environment.NewLine);

            var message = $"{resultMessage}\n\n" + 
                          $"Player Symbol: {strPlayerSymbol}\n" +
                          $"CPU Symbol: {strCPUSymbol}\n" +
                          $"Difficulty: {strDifficulty}\n" +
                          $"Session ID: {sessionId}\n" +
                          $"Final Board:\n\n" +
                          $"{board}\n".Replace("\n", Environment.NewLine);

            return message;

        }

        //Build the game start message when a session has been created, showing the initial board configuration
        public string buildSessionOpenMessage(TicTacToe ticTacToe){
            
            string strPlayerSymbol = (ticTacToe.playerSymbol == 'X' ? "X (First Turn)" : "O (Second Turn)");
            string strCPUSymbol = (ticTacToe.playerSymbol == 'X' ? "O (Second Turn)" : "X (First Turn)");
            string strDifficulty = (ticTacToe.difficulty == 1 ? "1 (Easy)" : (ticTacToe.difficulty == 2 ? "2 (Intermediate)" : "3 (Expert)"));
            string sessionId = ticTacToe.Id.ToString();
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

        //Build a message after a round has been completed showing the current board configuration
        public string buildSessionRoundMessage(TicTacToe updatedSession){

            string strPlayerSymbol = (updatedSession.playerSymbol == 'X' ? "X (First Turn)" : "O (Second Turn)");
            string strCPUSymbol = (updatedSession.playerSymbol == 'X' ? "O (Second Turn)" : "X (First Turn)");
            string strDifficulty = (updatedSession.difficulty == 1 ? "1 (Easy)" : (updatedSession.difficulty == 2 ? "2 (Intermediate)" : "3 (Expert)"));
            string sessionId = updatedSession.Id.ToString();
            char[][] currentBoard = updatedSession.board;

            string board = $"   |   |   \n" +
                           $" {currentBoard[0][0]} | {currentBoard[0][1]} | {currentBoard[0][2]} \n" +
                           $"_ _|_ _|_ _\n" +
                           $"   |   |   \n" +
                           $" {currentBoard[1][0]} | {currentBoard[1][1]} | {currentBoard[1][2]} \n" +
                           $"_ _|_ _|_ _\n" +
                           $"   |   |   \n" +
                           $" {currentBoard[2][0]} | {currentBoard[2][1]} | {currentBoard[2][2]} \n" +
                           $"   |   |   \n".Replace("\n", Environment.NewLine);

            var message = $"Nice move! Below is the current board at the end of the round:\n\n" + 
                          $"Player Symbol: {strPlayerSymbol}\n" +
                          $"CPU Symbol: {strCPUSymbol}\n" +
                          $"Difficulty: {strDifficulty}\n" +
                          $"Session ID: {sessionId}\n" +
                          $"Current Board:\n\n" +
                          $"{board}\n".Replace("\n", Environment.NewLine);

            return message;

        }

        //Determine the initial board returned to the player upon a game session being created
        //If the player chose to be X, a blank board is returned. If the player chose to be O,
        //the computer will make a move (move will differ depending on the difficulty selected)
        public char[][] determineInitialBoard(string symbol, int diff){

            char[][] board = new char[][] {new char[] {' ', ' ', ' '}, new char[] {' ', ' ', ' ',}, new char[] {' ', ' ', ' '}};

            //If player chose X, just return empty board
            if(symbol.Equals("X")){
                return board;
            }

            //CPU move will differ depending upon the difficulty selected
            //(Technically here, we could just call the brain of the CPU "decideCPUMove()" function.
            //however since the CPU using the minimax algorithm on difficulty 3 almost always chose the first position
            //that was the optimal move - in this case, position [0,0] - I decided to make the initial move just
            //a randomization of the most optimal first move(s) in Tic Tac Toe, which is ANY of the corners: [0,0], [2,0], [0,2], [2,2].
            //And likewise for difficullty 1 (easy), and 2 (intermediate) I chose the statistically worst move(s) and a mix of optimal and none
            //optimal moves, respectively.)
            if(diff == 1){
                int[][] moves = {new int[] {0, 1}, new int[] {1, 0}, new int[] {1, 2}, new int[] {2, 1}};
                int[] moveSet = moves[rand.Next(4)];
                board[moveSet[0]][moveSet[1]] = 'X';
            }else if(diff == 2){
                int[][] moves = {new int[] {0, 1}, new int[] {2, 1}, new int[] {1, 1}, new int[] {0, 0}, new int[] {2, 2}};
                int[] moveSet = moves[rand.Next(5)];
                board[moveSet[0]][moveSet[1]] = 'X';
            }else{
                int[][] moves = {new int[] {0, 0}, new int[] {2, 0}, new int[] {0, 2}, new int[] {2, 2}};
                int[] moveSet = moves[rand.Next(4)];
                board[moveSet[0]][moveSet[1]] = 'X';
            }

            return board;

        }

        //Play one round of a tic tac toe game, respective to which symbol the player has chosen
        public (char[][] board, int winner) playRound(TicTacToe openSession, int row, int column){

            char playerSymbol = openSession.playerSymbol;
            char computerSymbol = (playerSymbol == 'X' ? 'O' : 'X');
            int difficulty = openSession.difficulty;

            char[][] currentBoard = openSession.board;
            int winner = 2;

            int numberOfPositionsFilled = countPositionsFilled(currentBoard);

            //Players turn
            currentBoard[row][column] = playerSymbol;
            numberOfPositionsFilled++;

            //check if player wins or if board is filled
            if(checkWinner(currentBoard) == lookupTable[playerSymbol]){
                winner = lookupTable[playerSymbol];
                return (currentBoard, winner);
            }else if(numberOfPositionsFilled == 9){
                winner = 0;
                return (currentBoard, winner);
            }

            //Computers turn
            int[] cpuMoves = decideCPUMove(currentBoard, difficulty, computerSymbol);
            currentBoard[cpuMoves[0]][cpuMoves[1]] = computerSymbol;
            numberOfPositionsFilled++;

            //check if computer wins or if board is filled
            if(checkWinner(currentBoard) == lookupTable[computerSymbol]){
                winner = lookupTable[computerSymbol];
                return (currentBoard, winner);
            }else if(numberOfPositionsFilled == 9){
                winner = 0;
                return (currentBoard, winner);
            }

            return (currentBoard, winner);

        }

        //Brain of the computer player (CPU)
        //decideCPUMove handles the logic for deciding the next move of the CPU player,
        //which is totally dependent upon the difficulty selected. The difficulty selected
        //(specified by the difficulty parameter) is what determines how many levels deep the minimax
        //algorithm can traverse the tree of possible moves recursively (see the initialization of maxDepth for the equation).
        //The higer the difficulty, the deeper the minimax algorithm and can recursively traverse the tree of possible moves 
        //and outcomes, meaning the more likely the algorithm is to find the optimal sequence of moves needed to win the game,
        //(or at worst, draw).
        public int[] decideCPUMove(char[][] board, int difficulty, char symbol){
            int[] move = new int[] {0, 0};
            int bestScore = (symbol == 'X' ? int.MinValue : int.MaxValue);
            bool isMaximizer = (symbol == 'X' ? false : true);

            //determine the max depth based on difficulty
            int maxDepth = Math.Max(1, (9 - countPositionsFilled(board)) / (difficulty == 1 ? 3 : (difficulty == 2 ? 2 : 1)) - (difficulty == 3 ? 1 : 0));

            //This nest loop is techincally the current move of the CPU player for the current board configuration
            //The first call to minimax is made within
            for(int row = 0; row < board.Length; row++){
                for(int column = 0; column < board.Length; column++){
                    if(board[row][column] == ' '){
                        board[row][column] = symbol;
                        int score = minimax(board, row, column, symbol, 0, isMaximizer, maxDepth);
                        board[row][column] = ' ';

                        if(symbol == 'X'){
                            if(score > bestScore){
                                bestScore = score;
                                move[0] = row;
                                move[1] = column;
                            }
                        }else{
                            if(score < bestScore){
                                bestScore = score;
                                move[0] = row;
                                move[1] = column;
                            }
                        }
                    }
                }
            }

            return move;
        }

        //minimax is the algorithm that recursively traverses the entire tree of possible moves that can be made by the CPU, and
        //attempts to return the most optimal next move on the tic tac toe board
        public int minimax(char[][] board, int currRow, int currColumn, char symbol, int depth, bool isMaximizing, int maxDepth){
            
            //First, need to check if there is a winner with the current board configuration
            //If there is a winner, determine if it is the maximizer (X) or minimizer (O)
            int result = this.checkWinner(board);
            
            if(result != 2){
                return lookupTable[symbol] == 1 ? maxDepth - (depth - 1) : (maxDepth * -1) + (depth - 1);
                //return boardSize - (depth - 1) : (boardSize * -1) + (depth - 1));
            }else if(depth >= maxDepth){
                return lookupTable[symbol] == 1 ? maxDepth - (depth - 1) : (maxDepth * -1) + (depth - 1);
            }

            symbol = (symbol == 'X' ? 'O' : 'X');

            //If it is currently the maximizers move, find the maximum score out of the current available
            //moves. The highest score will determine the optimal move the maximizer (X) should make
            if(isMaximizing){
                int bestScore = int.MinValue;
                for(int row = 0; row < board.Length; row++){
                    for(int column = 0; column < board.Length; column++){
                        if(board[row][column] == ' '){
                            board[row][column] = symbol;
                            int score = minimax(board, row, column, symbol, depth + 1, !isMaximizing, maxDepth);
                            board[row][column] = ' ';
                            bestScore = Math.Max(score, bestScore);
                        }
                    }
                }
                return bestScore;
            //If it is currently the minimizers move, find the minimum score out of the current available
            //moves. The lowest score will determine the optimal move the minimizer (O) should make
            }else{
                int bestScore = int.MaxValue;
                for(int row = 0; row < board.Length; row++){
                    for(int column = 0; column < board.Length; column++){
                        if(board[row][column] == ' '){
                            board[row][column] = symbol;
                            int score = minimax(board, row, column, symbol, depth + 1, !isMaximizing, maxDepth);
                            board[row][column] = ' ';
                            bestScore = Math.Min(score, bestScore);
                        }
                    }
                }
                return bestScore;
            }

        }

        //Checks to see if the current board configuration contains a winner
        //If so, return 1 if maximizer won, or return -1 if minimizer won.
        //If the board if full and there is no winner, return 0 to declare a draw
        public int checkWinner(char[][] board){ //, int row, int column, char symbol){

            for(int count = 0; count < 3; count++){
                //Check rows
                if(threeInARow(board[count][0], board[count][1], board[count][2])){
                    return (board[count][0] == 'X' ? 1 : -1);
                }

                //Check columns
                if(threeInARow(board[0][count], board[1][count], board[2][count])){
                    return (board[0][count] == 'X' ? 1 : -1);
                }
            }

            if(threeInARow(board[0][0], board[1][1], board[2][2])){
                return (board[0][0] == 'X' ? 1 : -1);
            }

            if(threeInARow(board[2][0], board[1][1], board[0][2])){
                return (board[2][0] == 'X' ? 1 : -1);
            }

            int emptySpaces = 0;

            for(int row = 0; row < 3; row++){
                for(int column = 0; column < 3; column++){
                    if(board[row][column] == ' '){
                        emptySpaces++;
                    }
                }
            }

            return (emptySpaces > 0 ? 2 : 0);

            /*for(int column = 0; column < 3; column++){
                if(threeInARow(board[row][0], board[row][1], board[row][2])){
                    return (board[row][0] == 'x' || board[row][0] == 'X' ? 1 : -1);
                }
            }*/



            /*for(int count = 0; count < 3; count++){
                if(board[count][count] != symbol){
                    break;
                }
                if(count == 2){
                    return true;
                }
            }
        }

            if(row + column == 2){
                for(int count = 0; count < 3; count++){
                    if(board[count][2 - count] != symbol){
                        break;
                    }
                    if(count == 2){
                        return true;
                    }
                }
            }

            return false;*/
        }

        //Check if the passed in tic tac toe board positions contain the same X or O character
        public bool threeInARow(char a, char b, char c)
        {
            return a == b && b == c && a != ' ';
        }

        //Get the number of currently filled positions on the tic tac toe board
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