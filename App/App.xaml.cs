using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Configuration;
using System.IO;
using App.Helpers;
using App.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace App
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class AppClass : Application
    {
        // Текущая версия приложения (теперь берется из appsettings.json)
        private string _appVersion = "1.0.0"; // Значение по умолчанию
        
        public string AppVersion => _appVersion;
        
        // Менеджер обновлений
        private UpdateManager _updateManager;
        
        public AppClass()
        {
            // Загружаем настройки из файла при запуске
            _appVersion = SettingsManager.GetSetting<string>("Version", "1.0.0");
            Debug.WriteLine($"Текущая версия приложения: {_appVersion}");
            
            string? savedTheme = SettingsManager.GetSetting<string>("Theme", "Dark");
            if (!string.IsNullOrEmpty(savedTheme))
            {
                SwitchTheme(savedTheme);
            }
            
            // Загружаем настройки интерфейса
            bool enableAnimations = SettingsManager.GetSetting<bool>("EnableAnimations", true);
            bool enableRoundedCorners = SettingsManager.GetSetting<bool>("EnableRoundedCorners", true);
            
            // Применяем настройки
            SetEnableAnimations(enableAnimations);
            SetEnableRoundedCorners(enableRoundedCorners);
            
            // Создаем менеджер обновлений
            _updateManager = new UpdateManager(_appVersion);
            _updateManager.UpdateStatusChanged += UpdateManager_StatusChanged;
        }
        
        /// <summary>
        /// Переключает тему приложения и сохраняет выбор в настройках
        /// </summary>
        /// <param name="themeName">Имя темы (Dark или Light)</param>
        public static void SwitchTheme(string themeName)
        {
            var dictionaries = Current.Resources.MergedDictionaries;
            var themeUri = new Uri($"/App;component/Themes/{themeName}Theme.xaml", UriKind.Relative);
            
            // Удаляем текущую тему
            var existingTheme = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.ToString().Contains("/Themes/"));
            if (existingTheme != null)
            {
                dictionaries.Remove(existingTheme);
            }
            
            // Добавляем новую тему
            dictionaries.Add(new ResourceDictionary() { Source = themeUri });
            
            // Сохраняем настройку темы
            SettingsManager.SetSetting("Theme", themeName);
        }
        
        /// <summary>
        /// Получает ресурс текущей темы
        /// </summary>
        /// <param name="resourceKey">Ключ ресурса</param>
        /// <returns>Объект ресурса или null, если ресурс не найден</returns>
        public static object? GetThemeResource(string resourceKey)
        {
            var dictionaries = Current.Resources.MergedDictionaries;
            var themeDict = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.ToString().Contains("/Themes/"));
            
            if (themeDict != null && themeDict.Contains(resourceKey))
            {
                return themeDict[resourceKey];
            }
            
            return null;
        }
        
        /// <summary>
        /// Сохраняет все настройки приложения в файл конфигурации
        /// </summary>
        /// <param name="settings">Словарь с настройками для сохранения</param>
        public static void SaveAllSettings(Dictionary<string, object> settings)
        {
            SettingsManager.SaveAllSettings(settings);
            
            // Обрабатываем специальные настройки
            if (settings.TryGetValue("AutoStart", out object autoStartValue) && 
                settings.TryGetValue("StartMinimized", out object startMinimizedValue))
            {
                bool autoStart = Convert.ToBoolean(autoStartValue);
                bool startMinimized = Convert.ToBoolean(startMinimizedValue);
                
                // Настраиваем автозапуск
                AutoStartManager.SetAutoStart(autoStart, startMinimized);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Проверяем, запущено ли приложение с параметром --minimized
            if (AutoStartManager.ShouldStartMinimized())
            {
                // Если приложение запускается в свернутом режиме, то устанавливаем WindowState
                MainWindow.WindowState = WindowState.Minimized;
                // Можно также скрыть окно из панели задач, если нужно
                // MainWindow.ShowInTaskbar = false;
            }

            // Для тестирования - принудительно показываем диалог
            bool adminTest = false; // Измените на true для принудительного показа диалога

            // Проверка прав администратора
            if (!AdminHelper.IsRunningAsAdmin() || adminTest)
            {
                // Убираем сообщение о проверке прав
                // MessageBox.Show("Обнаружено, что приложение не запущено от имени администратора", 
                //     "Проверка прав", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Показываем диалоговое окно с загрузкой и вариантами запуска
                bool continueWithoutAdmin = AdminRequiredDialog.ShowAdminRequiredDialog();
                
                if (!continueWithoutAdmin)
                {
                    // Если пользователь выбрал "Закрыть", закрываем приложение
                    Shutdown();
                    return;
                }
                else
                {
                    // Если пользователь выбрал "Запустить с правами администратора", 
                    // запускаем процесс приложения с повышенными правами
                    try
                    {
                        // Получаем путь к текущему исполняемому файлу
                        string exePath = Process.GetCurrentProcess().MainModule.FileName;
                        
                        // Создаем новый процесс с повышенными правами
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true,
                            Verb = "runas" // Запуск с правами администратора
                        };
                        
                        // Запускаем процесс и завершаем текущий
                        Process.Start(startInfo);
                        Shutdown();
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось запустить приложение с правами администратора: {ex.Message}", 
                            "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                        return;
                    }
                }
            }
            
            // Проверяем обновления при запуске, если включено в настройках
            if (SettingsManager.Settings.CheckUpdatesAtStartup)
            {
                Dispatcher.BeginInvoke(new Action(async () => 
                {
                    // Даем приложению загрузиться полностью
                    await Task.Delay(2000);
                    
                    await CheckForUpdatesAsync();
                }));
            }
        }
        
        /// <summary>
        /// Проверяет наличие обновлений
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Начинаем проверку обновлений... Текущая версия: {_appVersion}");
                
                bool updateAvailable = await _updateManager.CheckForUpdatesAsync();
                
                // Если найдено обновление, показываем диалог
                if (updateAvailable && _updateManager.AvailableUpdate != null)
                {
                    // Загружаем список изменений
                    string changelog = await _updateManager.DownloadChangelogAsync();
                    
                    // Показываем диалог с информацией об обновлении
                    ShowUpdateDialog(_updateManager.AvailableUpdate.Version, changelog);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке обновлений: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Отображает диалог с предложением обновления
        /// </summary>
        private void ShowUpdateDialog(string version, string changelog)
        {
            var result = MessageBox.Show(
                $"Доступна новая версия {version}!\n\nОбновить сейчас?", 
                "Обновление доступно", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Information);
                
            if (result == MessageBoxResult.Yes)
            {
                // Создаем простое окно прогресса
                Window progressWindow = new Window
                {
                    Title = "Загрузка обновления",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.ToolWindow,
                    Topmost = true
                };
                
                Grid grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                
                TextBlock statusText = new TextBlock 
                { 
                    Text = "Загрузка обновления...", 
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                Grid.SetRow(statusText, 0);
                
                ProgressBar progressBar = new ProgressBar 
                { 
                    Width = 350, 
                    Height = 20, 
                    IsIndeterminate = true 
                };
                Grid.SetRow(progressBar, 2);
                
                grid.Children.Add(statusText);
                grid.Children.Add(progressBar);
                progressWindow.Content = grid;
                
                // Показываем окно
                progressWindow.Show();
                
                // Запускаем процесс обновления
                Dispatcher.BeginInvoke(new Action(async () => 
                {
                    try
                    {
                        // Отключаем неопределенный прогресс и подписываемся на события обновления прогресса
                        progressBar.IsIndeterminate = false;
                        progressBar.Minimum = 0;
                        progressBar.Maximum = 100;
                        progressBar.Value = 0;
                        
                        _updateManager.DownloadProgressChanged += (s, p) => {
                            Dispatcher.Invoke(() => {
                                progressBar.Value = p;
                                statusText.Text = $"Загрузка обновления... {p}%";
                            });
                        };
                        
                        // Загружаем обновление
                        bool downloadSuccess = await _updateManager.DownloadUpdateAsync();
                        
                        if (downloadSuccess)
                        {
                            statusText.Text = "Установка обновления...";
                            
                            // Небольшая задержка для визуального отображения
                            await Task.Delay(1000);
                            
                            // Закрываем окно прогресса перед установкой
                            progressWindow.Close();
                            
                            // Устанавливаем обновление
                            if (!_updateManager.InstallUpdate())
                            {
                                // Если установка не удалась
                                MessageBox.Show("Произошла ошибка при установке обновления.", 
                                    "Ошибка обновления", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Error);
                                
                                // Проверяем, существует ли файл с логами ошибок
                                string errorLogPath = Path.Combine(Path.GetTempPath(), "update_error.log");
                                if (File.Exists(errorLogPath))
                                {
                                    try
                                    {
                                        string errorContent = File.ReadAllText(errorLogPath);
                                        System.Diagnostics.Debug.WriteLine($"Содержимое лога ошибок: {errorContent}");
                                        
                                        MessageBox.Show($"Детали ошибки:\n{errorContent}", 
                                            "Подробности ошибки обновления", 
                                            MessageBoxButton.OK, 
                                            MessageBoxImage.Information);
                                            
                                        // Удаляем лог после прочтения
                                        File.Delete(errorLogPath);
                                    }
                                    catch (Exception logEx)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Ошибка при чтении лога: {logEx.Message}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Закрываем окно прогресса
                            progressWindow.Close();
                            
                            MessageBox.Show("Не удалось загрузить обновление. Попробуйте позже.", 
                                "Ошибка обновления", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Закрываем окно прогресса в случае ошибки
                        progressWindow.Close();
                        
                        MessageBox.Show($"Произошла ошибка при обновлении: {ex.Message}", 
                            "Ошибка обновления", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                            
                        System.Diagnostics.Debug.WriteLine($"Ошибка в ShowUpdateDialog: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Стек: {ex.StackTrace}");
                    }
                }));
            }
        }
        
        /// <summary>
        /// Обработчик изменения статуса обновления
        /// </summary>
        private void UpdateManager_StatusChanged(object? sender, App.Helpers.UpdateStatus updateStatus)
        {
            // При необходимости можно добавить дополнительную логику обработки статуса
            System.Diagnostics.Debug.WriteLine($"Статус обновления изменен: {updateStatus}");
        }
        
        /// <summary>
        /// Включает или отключает анимации в приложении
        /// </summary>
        /// <param name="enable">true - включить анимации, false - отключить</param>
        public static void SetEnableAnimations(bool enable)
        {
            try
            {
                Current.Resources["EnableAnimations"] = enable;
                
                // Сохраняем настройку
                SettingsManager.SetSetting("EnableAnimations", enable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке анимаций: {ex.Message}", 
                    "Ошибка настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Включает или отключает закругленные углы в приложении
        /// </summary>
        /// <param name="enable">true - включить закругленные углы, false - отключить</param>
        public static void SetEnableRoundedCorners(bool enable)
        {
            try
            {
                Current.Resources["EnableRoundedCorners"] = enable;
                
                // Устанавливаем значение скругления в зависимости от настройки
                CornerRadius cornerRadius = enable ? new CornerRadius(6) : new CornerRadius(0);
                Current.Resources["DefaultCornerRadius"] = cornerRadius;
                
                // Сохраняем настройку
                SettingsManager.SetSetting("EnableRoundedCorners", enable);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке скругления углов: {ex.Message}", 
                    "Ошибка настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Возвращает текущие настройки анимаций
        /// </summary>
        public static bool GetAnimationsEnabled()
        {
            try
            {
                if (Current.Resources.Contains("EnableAnimations"))
                {
                    return (bool)Current.Resources["EnableAnimations"];
                }
            }
            catch { }
            
            return SettingsManager.GetSetting<bool>("EnableAnimations", true);
        }
        
        /// <summary>
        /// Возвращает текущие настройки закругленных углов
        /// </summary>
        public static bool GetRoundedCornersEnabled()
        {
            try
            {
                if (Current.Resources.Contains("EnableRoundedCorners"))
                {
                    return (bool)Current.Resources["EnableRoundedCorners"];
                }
            }
            catch { }
            
            return SettingsManager.GetSetting<bool>("EnableRoundedCorners", true);
        }
    }
} 