using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Helpers;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace App.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        // Хранит информацию о текущих настройках
        private bool _isDarkTheme = true;
        private bool _enableAnimations = true;
        private bool _enableRoundedCorners = true;
        private bool _autoStart = false;
        private bool _startMinimized = false;
        private bool _checkUpdatesAtStartup = true;
        private bool _autoInstallUpdates = false;
        private string _currentTheme = "Dark";
        
        // Объект для работы с обновлениями
        private UpdateManager _updateManager;
        
        // Активная панель настроек
        private UIElement _currentPanel;
        
        // Текущая версия приложения
        private string _appVersion;
        
        // Добавляем флаг для отслеживания того, нужно ли открывать главное окно при закрытии
        private bool _openMainWindowOnClose = true;
        
        public SettingsWindow()
        {
            InitializeComponent();
            
            // Центрируем окно относительно активного монитора
            this.Loaded += SettingsWindow_Loaded;
            
            // Получаем версию из объекта приложения
            _appVersion = ((App.AppClass)Application.Current).AppVersion;
            
            // Загрузка текущих настроек
            LoadSettings();
            
            // Инициализация менеджера обновлений с текущей версией приложения
            _updateManager = new UpdateManager(_appVersion);
            
            // Запоминаем текущую активную панель
            _currentPanel = AppearancePanel;
            
            // Отображаем информацию о версии
            VersionInfoText.Text = $"Версия: {_appVersion}";
            CurrentVersionText.Text = $"Версия {_appVersion} (стабильная)";
            
            // Добавляем обработчик закрытия окна
            Closing += SettingsWindow_Closing;
        }
        
        /// <summary>
        /// Обработчик события загрузки окна
        /// </summary>
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Центрируем окно относительно активного монитора
            CenterWindowOnScreen();
        }
        
        /// <summary>
        /// Центрирует окно относительно активного монитора
        /// </summary>
        private void CenterWindowOnScreen()
        {
            try
            {
                // Получаем размеры экрана
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                
                // Центрируем окно относительно экрана
                this.Left = (screenWidth - this.Width) / 2;
                this.Top = (screenHeight - this.Height) / 2;
                
                // Убедимся, что окно полностью видимо
                if (this.Left < 0) this.Left = 0;
                if (this.Top < 0) this.Top = 0;
            }
            catch (Exception ex)
            {
                // В случае ошибки используем стандартное центрирование
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Debug.WriteLine($"Ошибка при центрировании окна: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Загружает настройки из SettingsManager
        /// </summary>
        private void LoadSettings()
        {
            // Загружаем данные о теме
            _isDarkTheme = SettingsManager.Settings.IsDarkTheme;
            _currentTheme = _isDarkTheme ? "Dark" : "Light";
            
            // Обновляем описание текущей темы
            ThemeDescription.Text = $"Текущая тема: {(_isDarkTheme ? "Темная" : "Светлая")}";
            
            // Загружаем данные об анимациях
            _enableAnimations = SettingsManager.Settings.EnableAnimations;
            EnableAnimationsToggle.IsChecked = _enableAnimations;
            
            // Загружаем данные о скруглённых углах
            _enableRoundedCorners = SettingsManager.Settings.EnableRoundedCorners;
            RoundedCornersToggle.IsChecked = _enableRoundedCorners;
            
            // Загружаем данные об автозапуске
            _autoStart = SettingsManager.Settings.AutoStart;
            AutoStartToggle.IsChecked = _autoStart;
            
            // Загружаем данные о запуске в свёрнутом виде
            _startMinimized = SettingsManager.Settings.StartMinimized;
            StartMinimizedToggle.IsChecked = _startMinimized;
            
            // Загружаем данные об обновлениях
            _checkUpdatesAtStartup = SettingsManager.Settings.CheckUpdatesAtStartup;
            CheckUpdatesToggle.IsChecked = _checkUpdatesAtStartup;
            
            // Установка текста о последней проверке обновлений
            if (SettingsManager.Settings.LastUpdateCheck != DateTime.MinValue)
            {
                LastCheckText.Text = $"Последняя проверка обновлений: {SettingsManager.Settings.LastUpdateCheck:dd.MM.yyyy HH:mm}";
            }
        }
        
        /// <summary>
        /// Обработчик для выпадающего списка выбора темы - заглушка (функционал временно отключен)
        /// </summary>
        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Функционал смены темы временно отключен
            // В будущих версиях здесь будет реализована смена темы
        }
        
        /// <summary>
        /// Применяет выбранную тему - заглушка (функционал временно отключен)
        /// </summary>
        private void ApplyTheme(string themeName)
        {
            // Функционал смены темы временно отключен
            // В будущих версиях здесь будет реализована смена темы
        }
        
        /// <summary>
        /// Обработчик для перетаскивания окна
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        
        /// <summary>
        /// Обработчик для кнопки закрытия
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        /// <summary>
        /// Обработчик для кнопок навигации
        /// </summary>
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            
            // Скрываем текущую активную панель
            _currentPanel.Visibility = Visibility.Collapsed;
            
            // Сбрасываем стили всех кнопок навигации
            AppearanceButton.Style = (Style)FindResource("SettingsNavButton");
            GeneralButton.Style = (Style)FindResource("SettingsNavButton");
            UpdatesButton.Style = (Style)FindResource("SettingsNavButton");
            AboutButton.Style = (Style)FindResource("SettingsNavButton");
            
            // Устанавливаем стиль выбранной кнопки
            clickedButton.Style = (Style)FindResource("SelectedNavButton");
            
            // Показываем нужную панель
            if (clickedButton == AppearanceButton)
            {
                AppearancePanel.Visibility = Visibility.Visible;
                _currentPanel = AppearancePanel;
            }
            else if (clickedButton == GeneralButton)
            {
                GeneralPanel.Visibility = Visibility.Visible;
                _currentPanel = GeneralPanel;
            }
            else if (clickedButton == UpdatesButton)
            {
                UpdatesPanel.Visibility = Visibility.Visible;
                _currentPanel = UpdatesPanel;
            }
            else if (clickedButton == AboutButton)
            {
                AboutPanel.Visibility = Visibility.Visible;
                _currentPanel = AboutPanel;
            }
        }
        
        /// <summary>
        /// Обработчик для переключателя анимаций
        /// </summary>
        private void AnimationsToggle_Checked(object sender, RoutedEventArgs e)
        {
            bool newValue = EnableAnimationsToggle.IsChecked == true;
            if (newValue != _enableAnimations)
            {
                _enableAnimations = newValue;
                
                // Применяем настройки анимаций
                App.AppClass.SetEnableAnimations(_enableAnimations);
                
                // Автоматически сохраняем все настройки
                var settings = new Dictionary<string, object>
                {
                    ["Theme"] = _currentTheme,
                    ["EnableAnimations"] = _enableAnimations,
                    ["EnableRoundedCorners"] = _enableRoundedCorners,
                    ["AutoStart"] = _autoStart,
                    ["StartMinimized"] = _startMinimized
                };
                
                App.AppClass.SaveAllSettings(settings);
            }
        }
        
        /// <summary>
        /// Обработчик для переключателя закругленных углов
        /// </summary>
        private void RoundedCornersToggle_Checked(object sender, RoutedEventArgs e)
        {
            bool newValue = RoundedCornersToggle.IsChecked == true;
            if (newValue != _enableRoundedCorners)
            {
                _enableRoundedCorners = newValue;
                
                // Применяем настройку закругленных углов
                App.AppClass.SetEnableRoundedCorners(_enableRoundedCorners);
                
                // Автоматически сохраняем все настройки
                var settings = new Dictionary<string, object>
                {
                    ["Theme"] = _currentTheme,
                    ["EnableAnimations"] = _enableAnimations,
                    ["EnableRoundedCorners"] = _enableRoundedCorners,
                    ["AutoStart"] = _autoStart,
                    ["StartMinimized"] = _startMinimized
                };
                
                App.AppClass.SaveAllSettings(settings);
            }
        }
        
        /// <summary>
        /// Обработчик для переключателя автозапуска
        /// </summary>
        private void AutoStartToggle_Checked(object sender, RoutedEventArgs e)
        {
            bool newValue = AutoStartToggle.IsChecked == true;
            if (newValue != _autoStart)
            {
                _autoStart = newValue;
                
                // Настраиваем автозапуск
                AutoStartManager.SetAutoStart(_autoStart, _startMinimized);
                
                // Автоматически сохраняем все настройки
                var settings = new Dictionary<string, object>
                {
                    ["Theme"] = _currentTheme,
                    ["EnableAnimations"] = _enableAnimations,
                    ["EnableRoundedCorners"] = _enableRoundedCorners,
                    ["AutoStart"] = _autoStart,
                    ["StartMinimized"] = _startMinimized
                };
                
                App.AppClass.SaveAllSettings(settings);
            }
        }
        
        /// <summary>
        /// Обработчик для переключателя запуска в свернутом состоянии
        /// </summary>
        private void StartMinimizedToggle_Checked(object sender, RoutedEventArgs e)
        {
            bool newValue = StartMinimizedToggle.IsChecked == true;
            if (newValue != _startMinimized)
            {
                _startMinimized = newValue;
                
                // Обновляем настройку автозапуска с новым значением свернутого запуска
                if (_autoStart)
                {
                    AutoStartManager.SetAutoStart(_autoStart, _startMinimized);
                }
                
                // Автоматически сохраняем все настройки
                var settings = new Dictionary<string, object>
                {
                    ["Theme"] = _currentTheme,
                    ["EnableAnimations"] = _enableAnimations,
                    ["EnableRoundedCorners"] = _enableRoundedCorners,
                    ["AutoStart"] = _autoStart,
                    ["StartMinimized"] = _startMinimized
                };
                
                App.AppClass.SaveAllSettings(settings);
            }
        }
        
        /// <summary>
        /// Обработчик для переключателя проверки обновлений при запуске
        /// </summary>
        private void CheckUpdatesToggle_Checked(object sender, RoutedEventArgs e)
        {
            bool newValue = CheckUpdatesToggle.IsChecked == true;
            if (newValue != _checkUpdatesAtStartup)
            {
                _checkUpdatesAtStartup = newValue;
                SettingsManager.Settings.CheckUpdatesAtStartup = _checkUpdatesAtStartup;
                SettingsManager.SaveSettings();
            }
        }
        
        /// <summary>
        /// Обработчик для кнопки проверки обновлений
        /// </summary>
        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync();
        }
        
        /// <summary>
        /// Проверяет наличие обновлений
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            // Показываем статус проверки
            UpdateStatusText.Text = "Проверка обновлений...";
            UpdateStatusText.Visibility = Visibility.Visible;
            CheckUpdateButton.IsEnabled = false;
            
            try
            {
                // Проверяем обновления
                bool updateAvailable = await _updateManager.CheckForUpdatesAsync();
                
                if (updateAvailable)
                {
                    // Загружаем список изменений
                    string changelog = await _updateManager.DownloadChangelogAsync();
                    
                    // Устанавливаем статусный текст - используем правильное свойство AvailableUpdate.Version
                    UpdateStatusText.Text = $"Доступно обновление {_updateManager.AvailableUpdate?.Version}";
                    
                    // Показываем кнопку установки
                    InstallUpdateButton.Visibility = Visibility.Visible;
                    InstallUpdateButton.IsEnabled = true;
                }
                else
                {
                    // Обновлений нет
                    UpdateStatusText.Text = "У вас установлена последняя версия";
                    InstallUpdateButton.Visibility = Visibility.Collapsed;
                }
                
                // Обновляем время последней проверки
                SettingsManager.Settings.LastUpdateCheck = DateTime.Now;
                SettingsManager.SaveSettings();
                
                LastCheckText.Text = $"Последняя проверка обновлений: {SettingsManager.Settings.LastUpdateCheck:dd.MM.yyyy HH:mm}";
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"Ошибка проверки обновлений: {ex.Message}";
                Debug.WriteLine($"Ошибка проверки обновлений: {ex}");
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }
        
        /// <summary>
        /// Обработчик для кнопки установки обновления
        /// </summary>
        private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем индикатор загрузки
            UpdateStatusText.Text = "Загрузка обновления...";
            UpdateStatusText.Visibility = Visibility.Visible;
            UpdateProgressBar.Visibility = Visibility.Visible;
            InstallUpdateButton.IsEnabled = false;
            CheckUpdateButton.IsEnabled = false;
            
            // Загружаем обновление
            bool downloadSuccess = await _updateManager.DownloadUpdateAsync();
            
            if (downloadSuccess)
            {
                UpdateStatusText.Text = "Установка обновления...";
                
                // Устанавливаем обновление
                _updateManager.InstallUpdate();
                // Примечание: если метод InstallUpdate выполнен успешно, приложение будет перезапущено
            }
            else
            {
                UpdateStatusText.Text = "Ошибка загрузки обновления!";
                InstallUpdateButton.IsEnabled = true;
                CheckUpdateButton.IsEnabled = true;
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку "Telegram"
        /// </summary>
        private void OpenTelegram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Открываем ссылку в браузере по умолчанию
                var telegramUrl = "https://t.me/+fzek1GcREpgwYmUy";
                Process.Start(new ProcessStartInfo(telegramUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть Telegram: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку "YouTube"
        /// </summary>
        private void OpenYouTube_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Открываем ссылку в браузере по умолчанию
                var youtubeUrl = "https://www.youtube.com/@GofMan3";
                Process.Start(new ProcessStartInfo(youtubeUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть YouTube: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку возврата в главное меню
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnToMainWindow();
        }
        
        /// <summary>
        /// Метод для возврата к главному окну
        /// </summary>
        private void ReturnToMainWindow()
        {
            // Создаем и показываем главное окно
            var mainWindow = new MainWindow();
            mainWindow.Show();
            
            // Устанавливаем флаг, чтобы не открывать главное окно повторно при закрытии
            _openMainWindowOnClose = false;
            
            // Закрываем текущее окно
            this.Close();
        }
        
        /// <summary>
        /// Обработчик события закрытия окна
        /// </summary>
        private void SettingsWindow_Closing(object sender, CancelEventArgs e)
        {
            // Если нужно, открываем главное окно
            if (_openMainWindowOnClose)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
        
        /// <summary>
        /// Обработчик изменения статуса обновления
        /// </summary>
        private void UpdateManager_StatusChanged(object? sender, App.Helpers.UpdateStatus status)
        {
            // Обрабатываем в потоке UI
            Dispatcher.Invoke(() => {
                switch (status)
                {
                    case App.Helpers.UpdateStatus.UpdateAvailable:
                        UpdateStatusText.Text = "Доступно новое обновление!";
                        break;
                    case App.Helpers.UpdateStatus.UpdateDownloading:
                        UpdateStatusText.Text = "Загрузка обновления...";
                        UpdateProgressBar.Visibility = Visibility.Visible;
                        break;
                    case App.Helpers.UpdateStatus.UpdateReady:
                        UpdateStatusText.Text = "Обновление готово к установке";
                        UpdateProgressBar.Visibility = Visibility.Collapsed;
                        break;
                    case App.Helpers.UpdateStatus.UpdateError:
                        UpdateStatusText.Text = "Ошибка при обновлении";
                        break;
                }
            });
        }
        
        /// <summary>
        /// Обработчик изменения прогресса загрузки обновления
        /// </summary>
        private void UpdateManager_ProgressChanged(object? sender, int progress)
        {
            Dispatcher.Invoke(() => {
                UpdateProgressBar.Value = progress;
            });
        }
    }
} 