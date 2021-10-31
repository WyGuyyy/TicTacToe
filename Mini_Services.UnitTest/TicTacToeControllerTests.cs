using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Mini_Services.api.Dtos;
using Mini_Services.api.Entities;
using Mini_Services.api.Repositories;
using Mini_Services.Api.Dtos;
using Moq;
using Xunit;
using System.Runtime.CompilerServices;
using Mini_Services.Api.Controllers;

namespace Mini_Services.UnitTest
{
    public class TicTacToeControllerTests
    {
         private readonly Mock<ITicTacToeRepository> repositoryStub = new();
         private readonly Mock<ILogger<TicTacToeController>> loggerStub = new();

         [Fact]
        public async Task CreateSessionAsync_WithInvalidParameters_ReturnsBadRequest()
        {

            TicTacToe ticTacToe = CreateRandomSession();

            //Arrange
            repositoryStub.Setup(repo => repo.CreateSessionAsync(ticTacToe));

            var controller = new TicTacToeController(repositoryStub.Object, loggerStub.Object);

            //Act
            var result1 = await controller.CreateSessionAsync("z", "1");
            var result2 = await controller.CreateSessionAsync("x", "0");
            var result3 = await controller.CreateSessionAsync("v", "4");

            //Assert
            result1.Result.Should().BeOfType<BadRequestObjectResult>();
            result1.Result.Should().BeOfType<BadRequestObjectResult>();
            result1.Result.Should().BeOfType<BadRequestObjectResult>();

        }

        [Fact]
        public async Task CreateSessionAsync_WithValidParameters_ReturnsTicTacToeDto()
        {

            TicTacToe ticTacToe = CreateRandomSession();

            //Arrange
            repositoryStub.Setup(repo => repo.CreateSessionAsync(ticTacToe));

            var controller = new TicTacToeController(repositoryStub.Object, loggerStub.Object);

            //Act
            var result1 = await controller.CreateSessionAsync("x", "1");
            //var result2 = await controller.CreateSessionAsync("o", "3");
            //var result3 = await controller.CreateSessionAsync("x", "2");

            //Assert
            //var session1 = result1.Result;
            //var session2 = (result2.Result as CreatedAtActionResult).Value as TicTacToeDto;
            //var session3 = (result3.Result as CreatedAtActionResult).Value as TicTacToeDto;

            //session1.sessionId.Should().NotBeEmpty();

            /*session1.sessionId.Should().NotBeEmpty();
            session2.sessionId.Should().NotBeEmpty();
            session3.sessionId.Should().NotBeEmpty();*/

        }

        private TicTacToe CreateRandomSession()
        {
            return new()
            {
                sessionId = new Guid(),
                board = new char[][]{new char[] {'x', 'x', 'x'}, new char[] {'o', 'o', 'o'}, new char[] {' ', ' ', ' '}},
                playerSymbol = 'x',
                difficulty = 1
            };
        }
    }
}