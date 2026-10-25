using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NexusDesk.Core.Services
{
    public interface IRegistryProvider
    {
        string? GetSteamPath();
    }

    public class WindowsRegistryProvider : IRegistryProvider
    {
        public string? GetSteamPath()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                    return key?.GetValue("SteamPath") as string;
                }
            }
            catch
            {
                // Ignore registry access errors
            }
            return null;
        }
    }

    public class GameScannerService
    {
        private readonly IRegistryProvider _registryProvider;

        public GameScannerService() : this(new WindowsRegistryProvider())
        {
        }

        public GameScannerService(IRegistryProvider registryProvider)
        {
            _registryProvider = registryProvider ?? throw new ArgumentNullException(nameof(registryProvider));
        }

        public async Task<List<GameInfo>> ScanLocalGamesAsync(string folderPath)
        {
            return await Task.Run(() =>
            {
                var games = new List<GameInfo>();
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    return games;
                }

                try
                {
                    var exeFiles = Directory.GetFiles(folderPath, "*.exe", SearchOption.AllDirectories);
                    foreach (var file in exeFiles)
                    {
                        if (IsLikelyGameExecutable(file))
                        {
                            games.Add(new GameInfo
                            {
                                Name = Path.GetFileNameWithoutExtension(file),
                                Path = file,
                                IconPath = file
                            });
                        }
                    }
                }
                catch
                {
                    // Handle access denied or IO exceptions
                }

                return games;
            });
        }

        public async Task<List<GameInfo>> ScanSteamGamesAsync()
        {
            return await Task.Run(async () =>
            {
                var games = new List<GameInfo>();
                var libraries = new List<string>();

                string? steamPath = _registryProvider.GetSteamPath();
                if (!string.IsNullOrEmpty(steamPath))
                {
                    steamPath = steamPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    if (Directory.Exists(steamPath))
                    {
                        libraries.Add(steamPath);
                        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                        if (File.Exists(vdfPath))
                        {
                            try
                            {
                                var content = await File.ReadAllTextAsync(vdfPath);
                                var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
                                foreach (Match match in matches)
                                {
                                    if (match.Groups.Count > 1)
                                    {
                                        var libPath = match.Groups[1].Value.Replace(@"\\", @"\").Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                                        if (Directory.Exists(libPath) && !libraries.Contains(libPath, StringComparer.OrdinalIgnoreCase))
                                        {
                                            libraries.Add(libPath);
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Ignore VDF read errors
                            }
                        }
                    }
                }

                foreach (var lib in libraries)
                {
                    var commonPath = Path.Combine(lib, "steamapps", "common");
                    if (Directory.Exists(commonPath))
                    {
                        try
                        {
                            var gameDirs = Directory.GetDirectories(commonPath);
                            foreach (var gameDir in gameDirs)
                            {
                                try
                                {
                                    var exeFiles = Directory.GetFiles(gameDir, "*.exe", SearchOption.AllDirectories);
                                    foreach (var file in exeFiles)
                                    {
                                        if (IsLikelyGameExecutable(file))
                                        {
                                            games.Add(new GameInfo
                                            {
                                                Name = Path.GetFileNameWithoutExtension(file),
                                                Path = file,
                                                IconPath = file
                                            });
                                        }
                                    }
                                }
                                catch
                                {
                                    // Skip inaccessible game dirs
                                }
                            }
                        }
                        catch
                        {
                            // Skip inaccessible common dirs
                        }
                    }
                }

                return games;
            });
        }

        private bool IsLikelyGameExecutable(string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLowerInvariant();
            if (fileName.Contains("unins") ||
                fileName.Contains("crash") ||
                fileName.Contains("setup") ||
                fileName.Contains("redist") ||
                fileName.Contains("dxsetup") ||
                fileName.Contains("vcredist") ||
                fileName.Contains("dotnetfx") ||
                fileName.Contains("unitycrashhandler") ||
                fileName.Contains("dxwebsetup") ||
                fileName.Contains("config"))
            {
                return false;
            }

            if (filePath.Contains("System32", StringComparison.OrdinalIgnoreCase) ||
                filePath.Contains("SysWOW64", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
