using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls; // <--- ТА САМАЯ НЕДОСТАЮЩАЯ СТРОКА
using System.Windows.Input;
using System.Windows.Media;
using NexusDesk.Core.Models;
using NexusDesk.Core.Services;
using NexusDesk.Host.Services;

namespace NexusDesk.Host
{
    public partial class MainWindow : Window
    {
        private AppConfig _config;
        private DiskScanner _diskScanner;
        private GameBoosterService _gameBooster;
        private GamepadService _gamepadService;

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

            _gamepadService = new GamepadService();
            _gamepadService.OnAction += GamepadService_OnAction;
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

        private void BigPicture_Checked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            BigPictureOverlay.Visibility = Visibility.Visible;
            MainMenuGrid.Visibility = Visibility.Collapsed;

            LoadGamesForBigPictureAsync();

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                var firstTile = FindVisualChild<Border>(BigPictureOverlay);
                firstTile?.Focus();
            }));
        }

        private async void LoadGamesForBigPictureAsync()
        {
            try
            {
                MyGamesStackPanel.Children.Clear();
                var scanner = new GameScannerService();

                var localGames = await scanner.ScanLocalGamesAsync(@"C:\Games");
                var steamGames = await scanner.ScanSteamGamesAsync();

                var allGames = localGames.Concat(steamGames)
                    .GroupBy(g => g.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                foreach (var game in allGames)
                {
                    var border = new Border
                    {
                        Style = (Style)FindResource("GameTileStyle")
                    };

                    var textBlock = new TextBlock
                    {
                        Text = game.Name,
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5)
                    };

                    border.Child = textBlock;

                    border.MouseLeftButtonUp += (s, e) =>
                    {
                        LaunchGame(game);
                    };

                    border.KeyDown += (s, e) =>
                    {
                        if (e.Key == Key.Enter)
                        {
                            LaunchGame(game);
                            e.Handled = true;
                        }
                    };

                    MyGamesStackPanel.Children.Add(border);
                }

                if (allGames.Count == 0)
                {
                    var emptyText = new TextBlock
                    {
                        Text = "Игры не найдены",
                        FontSize = 16,
                        Foreground = Brushes.Gray,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10)
                    };
                    MyGamesStackPanel.Children.Add(emptyText);
                }
            }
            catch (Exception)
            {
                // Ignore scanning errors
            }
        }

        private void LaunchGame(GameInfo game)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = game.Path,
                    WorkingDirectory = Path.GetDirectoryName(game.Path) ?? string.Empty,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось запустить игру {game.Name}: {ex.Message}");
            }
        }

        private void BigPicture_Unchecked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            BigPictureOverlay.Visibility = Visibility.Collapsed;
            MainMenuGrid.Visibility = Visibility.Visible;
        }

        private void BigPictureExit_Click(object sender, RoutedEventArgs e)
        {
            if (_config != null && _config.Modules.ContainsKey("bigPicture"))
            {
                _config.Modules["bigPicture"].Enabled = false;
            }
            BigPicture_Unchecked(sender, e);
        }

        private void GamepadService_OnAction(GamepadAction action)
        {
            Dispatcher.Invoke(() =>
            {
                if (BigPictureOverlay.Visibility != Visibility.Visible) return;

                switch (action)
                {
                    case GamepadAction.Left:
                        MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                        break;
                    case GamepadAction.Right:
                        MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        break;
                    case GamepadAction.A:
                        if (Keyboard.FocusedElement is UIElement focusedElement)
                        {
                            var downArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, Key.Enter)
                            {
                                RoutedEvent = Keyboard.KeyDownEvent
                            };
                            focusedElement.RaiseEvent(downArgs);

                            var upArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, Key.Enter)
                            {
                                RoutedEvent = Keyboard.KeyUpEvent
                            };
                            focusedElement.RaiseEvent(upArgs);
                        }
                        break;
                    case GamepadAction.B:
                        if (_config != null && _config.Modules.ContainsKey("bigPicture"))
                        {
                            _config.Modules["bigPicture"].Enabled = false;
                        }
                        BigPicture_Unchecked(this, new RoutedEventArgs());
                        break;
                }
            });
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            _gamepadService?.Stop();
            // Сохраняем конфиг при закрытии
            ConfigManager.Save(_config);
            base.OnClosed(e);
        }
    }
}