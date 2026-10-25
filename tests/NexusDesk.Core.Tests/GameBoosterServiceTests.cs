using System.Collections.Generic;
using System.Threading.Tasks;
using NexusDesk.Core.Services;
using Xunit;

namespace NexusDesk.Core.Tests
{
    public class GameBoosterServiceTests
    {
        private class TestProcessExecutor : IProcessExecutor
        {
            public List<(string FileName, string Arguments)> ExecutedCalls { get; } = new();

            public Task ExecuteAsync(string fileName, string arguments)
            {
                ExecutedCalls.Add((fileName, arguments));
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task EnableBoostAsync_ExecutesClearStandbyListAndStopsWSearch()
        {
            // Arrange
            var executor = new TestProcessExecutor();
            var service = new GameBoosterService(executor);

            // Act
            await service.EnableBoostAsync();

            // Assert
            Assert.Equal(2, executor.ExecutedCalls.Count);
            Assert.Equal("EmptyStandbyList.exe", executor.ExecutedCalls[0].FileName);
            Assert.Equal("standbylist", executor.ExecutedCalls[0].Arguments);
            
            Assert.Equal("sc.exe", executor.ExecutedCalls[1].FileName);
            Assert.Equal("stop WSearch", executor.ExecutedCalls[1].Arguments);
        }

        [Fact]
        public async Task DisableBoostAsync_StartsWSearch()
        {
            // Arrange
            var executor = new TestProcessExecutor();
            var service = new GameBoosterService(executor);

            // Act
            await service.DisableBoostAsync();

            // Assert
            Single(executor.ExecutedCalls);
            Assert.Equal("sc.exe", executor.ExecutedCalls[0].FileName);
            Assert.Equal("start WSearch", executor.ExecutedCalls[0].Arguments);
        }

        private static void Single<T>(List<T> list)
        {
            Assert.Single(list);
        }
    }
}
