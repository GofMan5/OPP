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
using System.Windows.Media;
using System.Windows.Media.Animation;

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
                
                // Проверяем, не запущена ли уже проверка обновлений
                if (_updateManager.IsCheckingForUpdates)
                {
                    System.Diagnostics.Debug.WriteLine("Проверка обновлений уже выполняется. Пропускаем.");
                    return;
                }
                
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
            // Создаем красивое модальное окно для обновления
            Window updateWindow = new Window
            {
                Title = "Доступно обновление",
                Width = 500,
                Height = 380,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true
            };
            
            // Создаем главный контейнер с тенью
            Border mainBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E2B37")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(10)
            };
            
            // Добавляем эффект тени
            mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = 5,
                BlurRadius = 10,
                Opacity = 0.5
            };
            
            // Создаем основной Grid
            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) }); // Заголовок
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Содержимое
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) }); // Кнопки
            
            // Заголовок с возможностью перетаскивания
            Border headerBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                CornerRadius = new CornerRadius(10, 10, 0, 0)
            };
            
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            TextBlock titleText = new TextBlock
            {
                Text = "Доступно обновление",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 0, 0)
            };
            
            Button closeButton = new Button
            {
                Content = "✕",
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Width = 40,
                Height = 40,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            closeButton.Click += (s, e) => updateWindow.Close();
            
            Grid.SetColumn(titleText, 0);
            Grid.SetColumn(closeButton, 1);
            
            headerGrid.Children.Add(titleText);
            headerGrid.Children.Add(closeButton);
            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            
            // Добавляем возможность перетаскивания окна
            headerBorder.MouseLeftButtonDown += (s, e) => 
            {
                updateWindow.DragMove();
            };
            
            // Содержимое
            ScrollViewer contentScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(20)
            };
            
            StackPanel contentPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 10, 0)
            };
            
            // Иконка обновления
            System.Windows.Shapes.Path updateIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"),
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                Width = 40,
                Height = 40,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            // Информация о новой версии
            TextBlock versionText = new TextBlock
            {
                Text = $"Новая версия {version}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            // Добавляем анимацию для заголовка
            ScaleTransform scaleTransform = new ScaleTransform(1, 1);
            versionText.RenderTransform = scaleTransform;
            versionText.RenderTransformOrigin = new Point(0.5, 0.5);
            
            DoubleAnimation pulseAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.05,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
            
            // Сообщение об обновлении
            TextBlock messageText = new TextBlock
            {
                Text = _updateManager.AvailableUpdate?.UpdateMessages?.Ru ?? "Рекомендуется установить для улучшения функциональности и стабильности.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            // Раздел "Что нового"
            TextBlock whatNewTitle = new TextBlock
            {
                Text = "Что нового:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            
            TextBlock changelogText = new TextBlock
            {
                Text = changelog,
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(10, 0, 0, 15)
            };
            
            // Размер обновления
            TextBlock sizeText = null;
            if (_updateManager.AvailableUpdate?.FileSize > 0)
            {
                double sizeInMb = Math.Round(_updateManager.AvailableUpdate.FileSize / 1024.0 / 1024.0, 2);
                sizeText = new TextBlock
                {
                    Text = $"Размер: {sizeInMb} МБ",
                    Foreground = Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 0, 5)
                };
            }
            
            // Добавляем элементы в контейнер содержимого
            contentPanel.Children.Add(updateIcon);
            contentPanel.Children.Add(versionText);
            contentPanel.Children.Add(messageText);
            contentPanel.Children.Add(whatNewTitle);
            contentPanel.Children.Add(changelogText);
            
            if (sizeText != null)
                contentPanel.Children.Add(sizeText);
                
            contentScroll.Content = contentPanel;
            Grid.SetRow(contentScroll, 1);
            
            // Панель с кнопками
            Grid buttonGrid = new Grid
            {
                Margin = new Thickness(20, 0, 20, 15)
            };
            
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            Button installButton = new Button
            {
                Content = "Установить сейчас",
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            
            // Стиль для кнопки
            installButton.Style = new Style(typeof(Button));
            installButton.Style.Setters.Add(new Setter(Button.TemplateProperty, 
                Application.Current.Resources["RoundedButtonTemplate"] as ControlTemplate ?? 
                Application.Current.Resources["ButtonTemplate"] as ControlTemplate));
            
            // Обработчик для кнопки установки
            installButton.Click += async (s, e) => 
            {
                updateWindow.Close();
                await DownloadAndInstallUpdateAsync();
            };
            
            Button laterButton = new Button
            {
                Content = "Позже",
                Padding = new Thickness(20, 10, 20, 10),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"))
            };
            
            // Стиль для кнопки
            laterButton.Style = new Style(typeof(Button));
            laterButton.Style.Setters.Add(new Setter(Button.TemplateProperty, 
                Application.Current.Resources["RoundedButtonTemplate"] as ControlTemplate ?? 
                Application.Current.Resources["ButtonTemplate"] as ControlTemplate));
            
            // Обработчик для кнопки "Позже"
            laterButton.Click += (s, e) => updateWindow.Close();
            
            Grid.SetColumn(installButton, 1);
            Grid.SetColumn(laterButton, 2);
            
            buttonGrid.Children.Add(installButton);
            buttonGrid.Children.Add(laterButton);
            Grid.SetRow(buttonGrid, 2);
            
            // Добавляем все элементы в основной Grid
            mainGrid.Children.Add(headerBorder);
            mainGrid.Children.Add(contentScroll);
            mainGrid.Children.Add(buttonGrid);
            
            mainBorder.Child = mainGrid;
            updateWindow.Content = mainBorder;
            
            // Анимация появления окна
            updateWindow.Opacity = 0;
            
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            
            updateWindow.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            
            // Показываем окно
            updateWindow.ShowDialog();
        }
        
        /// <summary>
        /// Загружает и устанавливает обновление
        /// </summary>
        private async Task DownloadAndInstallUpdateAsync()
        {
            // Создаем окно с прогрессом загрузки
            Window progressWindow = new Window
            {
                Title = "Загрузка обновления",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true
            };
            
            // Создаем главный контейнер с тенью
            Border mainBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E2B37")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10)
            };
            
            // Добавляем эффект тени
            mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = 5,
                BlurRadius = 10,
                Opacity = 0.5
            };
            
            Grid grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            
            TextBlock statusText = new TextBlock 
            { 
                Text = "Загрузка обновления...", 
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(statusText, 0);
            
            ProgressBar progressBar = new ProgressBar 
            { 
                Width = 350, 
                Height = 10, 
                IsIndeterminate = false,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A3F55")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"))
            };
            Grid.SetRow(progressBar, 2);
            
            grid.Children.Add(statusText);
            grid.Children.Add(progressBar);
            mainBorder.Child = grid;
            progressWindow.Content = mainBorder;
            
            // Анимация появления окна
            progressWindow.Opacity = 0;
            
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            
            progressWindow.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            
            // Показываем окно
            progressWindow.Show();
            
            // Запускаем процесс обновления
            Dispatcher.BeginInvoke(new Action(async () => 
            {
                try
                {
                    // Подписываемся на события обновления прогресса
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
                                // Показываем содержимое лог-файла
                                string errorLog = File.ReadAllText(errorLogPath);
                                MessageBox.Show($"Содержимое лога ошибок:\n\n{errorLog}", 
                                    "Детали ошибки обновления", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Information);
                            }
                        }
                    }
                    else
                    {
                        // Закрываем окно прогресса в случае ошибки
                        progressWindow.Close();
                        
                        MessageBox.Show("Не удалось загрузить обновление. Пожалуйста, попробуйте позже.", 
                            "Ошибка загрузки", 
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