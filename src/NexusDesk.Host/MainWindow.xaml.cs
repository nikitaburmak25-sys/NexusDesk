using System.Windows;
using NexusDesk.Core.Models;

namespace NexusDesk.Host
{
    public partial class MainWindow : Window
    {
        private AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();
            
            // Загружаем конфиг при старте
            _config = ConfigManager.Load();
            this.DataContext = _config;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            // Сохраняем конфиг при закрытии
            ConfigManager.Save(_config);
            base.OnClosed(e);
        }
    }
}