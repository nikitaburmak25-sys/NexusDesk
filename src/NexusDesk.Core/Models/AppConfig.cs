using System;
using System.Collections.Generic;

namespace NexusDesk.Core.Models
{
    public enum UiPreset
    {
        Performance,
        Beauty
    }

    public enum ThemeMode
    {
        Light,
        Dark,
        System
    }

    public class ModuleConfig
    {
        public bool Enabled { get; set; } = false;
        public bool RequiresAdmin { get; set; } = false;
    }

    public class AppConfig
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string Locale { get; set; } = "auto";
        public UiPreset UiPreset { get; set; } = UiPreset.Performance;
        public ThemeMode Theme { get; set; } = ThemeMode.System;
        
        public Dictionary<string, ModuleConfig> Modules { get; set; } = new()
        {
            { "sorter", new ModuleConfig { Enabled = true } },
            { "gameBooster", new ModuleConfig { Enabled = false, RequiresAdmin = true } },
            { "bigPicture", new ModuleConfig { Enabled = false } }
        };
    }
}