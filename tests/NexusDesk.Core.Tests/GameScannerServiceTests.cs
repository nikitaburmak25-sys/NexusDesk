using System.IO;
using System.Threading.Tasks;
using NexusDesk.Core.Services;
using Xunit;

namespace NexusDesk.Core.Tests
{
    public class GameScannerServiceTests
    {
        private class MockRegistryProvider : IRegistryProvider
        {
            private readonly string? _steamPath;

            public MockRegistryProvider(string? steamPath)
            {
                _steamPath = steamPath;
            }

            public string? GetSteamPath() => _steamPath;
        }

        [Fact]
        public async Task ScanLocalGamesAsync_FindsExesAndFiltersUnwanted()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "NexusDeskTest_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var gameExe = Path.Combine(tempDir, "AwesomeGame.exe");
                var uninstallerExe = Path.Combine(tempDir, "unins000.exe");
                var textFile = Path.Combine(tempDir, "readme.txt");

                File.WriteAllText(gameExe, "dummy");
                File.WriteAllText(uninstallerExe, "dummy");
                File.WriteAllText(textFile, "text");

                var service = new GameScannerService();
                var games = await service.ScanLocalGamesAsync(tempDir);

                Assert.Single(games);
                Assert.Equal("AwesomeGame", games[0].Name);
                Assert.Equal(gameExe, games[0].Path);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task ScanSteamGamesAsync_FindsSteamGames()
        {
            var steamDir = Path.Combine(Path.GetTempPath(), "NexusDeskSteam_" + Path.GetRandomFileName());
            var commonDir = Path.Combine(steamDir, "steamapps", "common", "Cyberpunk");
            Directory.CreateDirectory(commonDir);

            try
            {
                var cyberpunkExe = Path.Combine(commonDir, "Cyberpunk2077.exe");
                File.WriteAllText(cyberpunkExe, "dummy");

                var registry = new MockRegistryProvider(steamDir);
                var service = new GameScannerService(registry);

                var games = await service.ScanSteamGamesAsync();

                Assert.Single(games);
                Assert.Equal("Cyberpunk2077", games[0].Name);
                Assert.Equal(cyberpunkExe, games[0].Path);
            }
            finally
            {
                if (Directory.Exists(steamDir))
                {
                    Directory.Delete(steamDir, true);
                }
            }
        }
    }
}
