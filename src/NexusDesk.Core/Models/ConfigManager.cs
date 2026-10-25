using System;
using System.IO;
using Newtonsoft.Json;

namespace NexusDesk.Core.Models
{
    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "systemcore.json");

        public static AppConfig Load()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new AppConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                // Если файл поврежден, возвращаем дефолтный
                return new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения конфига: {ex.Message}");
            }
        }
    }
}