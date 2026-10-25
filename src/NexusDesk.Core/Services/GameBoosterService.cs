using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusDesk.Core.Services
{
    public interface IProcessExecutor
    {
        Task ExecuteAsync(string fileName, string arguments);
    }

    public class DefaultProcessExecutor : IProcessExecutor
    {
        public async Task ExecuteAsync(string fileName, string arguments)
        {
            await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit();
                }
            });
        }
    }

    public class GameBoosterService
    {
        private readonly IProcessExecutor _processExecutor;

        public GameBoosterService() : this(new DefaultProcessExecutor())
        {
        }

        public GameBoosterService(IProcessExecutor processExecutor)
        {
            _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        }

        public async Task EnableBoostAsync()
        {
            try
            {
                await _processExecutor.ExecuteAsync("EmptyStandbyList.exe", "standbylist");
            }
            catch
            {
                // EmptyStandbyList.exe might not be present on all systems
            }

            await _processExecutor.ExecuteAsync("sc.exe", "stop WSearch");
        }

        public async Task DisableBoostAsync()
        {
            await _processExecutor.ExecuteAsync("sc.exe", "start WSearch");
        }
    }
}
