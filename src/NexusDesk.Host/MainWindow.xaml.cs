using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using NexusDesk.Core.Models;
using NexusDesk.Core.Services;

namespace NexusDesk.Host
{
    public partial class MainWindow : Window
    {
        private AppConfig _config;
        private DiskScanner _diskScanner;
        private GameBoosterService _gameBooster;

        public ObservableCollection<DriveInfo> Drives { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            
            // Загружаем конфиг при старте
            _config = ConfigManager.Load();
            this.DataContext = _config;

            _diskScanner = new DiskScanner();
            _gameBooster = new GameBoosterService();
            LoadDrivesAsync();
        }

        private async void LoadDrivesAsync()
        {
            var drives = await _diskScanner.GetDrivesAsync();
            foreach (var drive in drives)
            {
                Drives.Add(drive);
            }
        }

        private async void SortButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                var sorter = new FileSorterService();
                await sorter.ProcessDirectoryAsync(downloadsPath);
                MessageBox.Show("Сортировка завершена!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void GameBooster_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _gameBooster.EnableBoostAsync();
                MessageBox.Show("Game Booster: Максимальная производительность включена");
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось изменить состояние службы. Запустите программу от имени администратора");
            }
        }

        private async void GameBooster_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _gameBooster.DisableBoostAsync();
                MessageBox.Show("Game Booster: Сбалансированный режим восстановлен");
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось изменить состояние службы. Запустите программу от имени администратора");
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            // Сохраняем конфиг при закрытии
            ConfigManager.Save(_config);
            base.OnClosed(e);
        }
    }
}