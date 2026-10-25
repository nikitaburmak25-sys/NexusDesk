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

        public ObservableCollection<DriveInfo> Drives { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            
            // Загружаем конфиг при старте
            _config = ConfigManager.Load();
            this.DataContext = _config;

            _diskScanner = new DiskScanner();
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

        protected override void OnClosed(System.EventArgs e)
        {
            // Сохраняем конфиг при закрытии
            ConfigManager.Save(_config);
            base.OnClosed(e);
        }
    }
}