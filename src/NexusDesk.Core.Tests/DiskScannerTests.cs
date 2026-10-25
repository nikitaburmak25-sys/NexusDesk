using System.Threading.Tasks;
using Xunit;
using NexusDesk.Core.Services;

namespace NexusDesk.Core.Tests
{
    public class DiskScannerTests
    {
        [Fact]
        public async Task GetDrivesAsync_ReturnsDrivesList()
        {
            var scanner = new DiskScanner();
            var drives = await scanner.GetDrivesAsync();

            Assert.NotNull(drives);
        }
    }
}
