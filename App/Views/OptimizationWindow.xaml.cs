using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Management;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Interop;
using System.Windows.Media;

namespace App.Views
{
    /// <summary>
    /// Класс для представления сетевого адаптера
    /// </summary>
    public class NetworkAdapterInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
    
    /// <summary>
    /// Логика взаимодействия для OptimizationWindow.xaml
    /// </summary>
    public partial class OptimizationWindow : Window
    {
        // Инициализируем таймер для анимации
        private readonly DispatcherTimer _dotsAnimationTimer = new DispatcherTimer();
        private int _dotAnimationStep = 0;
        private Storyboard? _loadingRotationStoryboard;
        
        // Таймер для автоматического обновления характеристик
        private readonly DispatcherTimer _systemInfoUpdateTimer = new DispatcherTimer();
        private DateTime _lastUpdateTime;
        
        // Флаг для отслеживания полноэкранного режима
        private bool _isFullScreen = false;
        
        // Для хранения данных о сетевых адаптерах
        private readonly List<NetworkAdapterInfo> _networkAdapters = new List<NetworkAdapterInfo>();
        
        // Сохраняем исходные размеры и положение окна
        private double _originalWidth;
        private double _originalHeight;
        private WindowState _originalWindowState;
        private WindowStyle _originalWindowStyle;
        private ResizeMode _originalResizeMode;
        private bool _originalAllowsTransparency;
        private Thickness _originalBorderThickness;
        
        // Добавляем флаг для отслеживания того, нужно ли открывать главное окно при закрытии
        private bool _openMainWindowOnClose = true;
        
        public OptimizationWindow()
        {
            InitializeComponent();
            
            // Настройка анимаций загрузки
            SetupLoadingAnimations();
            
            // Настройка таймера обновления
            SetupSystemInfoUpdateTimer();
            
            // Загружаем информацию о системе при загрузке окна
            Loaded += OptimizationWindow_Loaded;
            
            // Добавляем обработчик закрытия окна
            Closing += OptimizationWindow_Closing;
        }

        /// <summary>
        /// Настройка таймера автоматического обновления характеристик
        /// </summary>
        private void SetupSystemInfoUpdateTimer()
        {
            _systemInfoUpdateTimer.Interval = TimeSpan.FromMinutes(1);
            _systemInfoUpdateTimer.Tick += (s, e) => {
                GetSystemInfo();
                UpdateLastUpdateTime();
            };
            _systemInfoUpdateTimer.Start();
        }
        
        /// <summary>
        /// Обновляет время последнего обновления характеристик
        /// </summary>
        private void UpdateLastUpdateTime()
        {
            _lastUpdateTime = DateTime.Now;
            
            // Форматируем время в понятный вид
            string timeStr = _lastUpdateTime.ToString("HH:mm:ss");
            LastUpdateText.Text = $"Обновлено: {timeStr}";
        }

        /// <summary>
        /// Обработка события загрузки окна
        /// </summary>
        private async void OptimizationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Центрируем окно относительно активного монитора
            CenterWindowOnScreen();
            
            // Показываем индикатор загрузки
            ShowLoadingScreen("Сбор информации о системе...");
            
            try
            {
                // Получаем информацию о системе асинхронно
                await Task.Run(() => GetSystemInfo());
                UpdateLastUpdateTime();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении информации о системе: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Скрываем индикатор загрузки
                HideLoadingScreen();
            }
        }
        
        /// <summary>
        /// Получение информации о системе
        /// </summary>
        private void GetSystemInfo()
        {
            try
            {
                // Вызываем методы получения информации в интерфейсном потоке
                Dispatcher.Invoke(() => 
                {
                    GetOSInfo();
                    GetCPUInfo();
                    GetRAMInfo();
                    GetGPUInfo();
                    GetNetworkInfo();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении информации о системе: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Получение информации об операционной системе
        /// </summary>
        private void GetOSInfo()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject os in searcher.Get().Cast<ManagementObject>())
                    {
                        string caption = os["Caption"]?.ToString() ?? "Неизвестно";
                        
                        // Получаем версию и сборку
                        string osVersion = Environment.OSVersion.Version.ToString();
                        
                        // Получаем битность ОС
                        string architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
                        
                        // Обновляем элемент интерфейса
                        OSVersionText.Text = $"{caption} {architecture} (версия {osVersion})";
                        return;
                    }
                }
                
                OSVersionText.Text = "Не удалось получить информацию";
            }
            catch (Exception ex)
            {
                OSVersionText.Text = $"Ошибка получения данных: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Получение информации о процессоре
        /// </summary>
        private void GetCPUInfo()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                {
                    foreach (ManagementObject processor in searcher.Get().Cast<ManagementObject>())
                    {
                        // Получаем модель процессора
                        string name = processor["Name"]?.ToString() ?? "Неизвестно";
                        CPUModelText.Text = name;
                        
                        // Получаем количество ядер
                        string cores = processor["NumberOfCores"]?.ToString() ?? "Неизвестно";
                        CPUCoresText.Text = cores;
                        
                        // Получаем количество логических процессоров (потоков)
                        string threads = processor["NumberOfLogicalProcessors"]?.ToString() ?? "Неизвестно";
                        CPUThreadsText.Text = threads;
                        
                        break;
                    }
                }
                
                // Если не удалось получить информацию о процессоре
                if (string.IsNullOrEmpty(CPUModelText.Text) || CPUModelText.Text == "Неизвестно")
                {
                    CPUModelText.Text = "Не удалось получить информацию";
                    CPUCoresText.Text = "Не удалось получить информацию";
                    CPUThreadsText.Text = "Не удалось получить информацию";
                }
            }
            catch (Exception ex)
            {
                CPUModelText.Text = $"Ошибка получения данных: {ex.Message}";
                CPUCoresText.Text = "Ошибка получения данных";
                CPUThreadsText.Text = "Ошибка получения данных";
            }
        }
        
        /// <summary>
        /// Получение информации об оперативной памяти
        /// </summary>
        private void GetRAMInfo()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject os in searcher.Get().Cast<ManagementObject>())
                    {
                        // Получаем общий объем памяти
                        if (os["TotalVisibleMemorySize"] != null)
                        {
                            ulong totalMemoryKB = Convert.ToUInt64(os["TotalVisibleMemorySize"]);
                            double totalMemoryGB = totalMemoryKB / 1024.0 / 1024.0;
                            RAMTotalText.Text = $"{totalMemoryGB:F2} ГБ";
                        }
                        else
                        {
                            RAMTotalText.Text = "Неизвестно";
                        }
                        
                        // Получаем объем доступной памяти
                        if (os["FreePhysicalMemory"] != null)
                        {
                            ulong freeMemoryKB = Convert.ToUInt64(os["FreePhysicalMemory"]);
                            double freeMemoryGB = freeMemoryKB / 1024.0 / 1024.0;
                            RAMAvailableText.Text = $"{freeMemoryGB:F2} ГБ";
                        }
                        else
                        {
                            RAMAvailableText.Text = "Неизвестно";
                        }
                        
                        return;
                    }
                }
                
                RAMTotalText.Text = "Не удалось получить информацию";
                RAMAvailableText.Text = "Не удалось получить информацию";
            }
            catch (Exception ex)
            {
                RAMTotalText.Text = $"Ошибка получения данных: {ex.Message}";
                RAMAvailableText.Text = "Ошибка получения данных";
            }
        }
        
        /// <summary>
        /// Получение информации о видеокарте и мониторе
        /// </summary>
        private void GetGPUInfo()
        {
            try
            {
                bool videoMemoryFound = false;
                
                // Сначала пробуем получить через более подробный WMI запрос, который может дать более точную информацию
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_VideoController"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            string name = obj["Name"]?.ToString() ?? "Неизвестно";
                            
                            // Если это нужная нам видеокарта (NVIDIA)
                            if (name.Contains("NVIDIA") || name.Contains("GeForce"))
                            {
                                GPUModelText.Text = name;
                                
                                // Проверяем все возможные свойства, где может храниться информация о видеопамяти
                                if (obj["AdapterRAM"] != null)
                                {
                                    ulong adapterRAM = Convert.ToUInt64(obj["AdapterRAM"]);
                                    double vramGB = adapterRAM / 1024.0 / 1024.0 / 1024.0;
                                    
                                    // Для карт RTX 3060 явно задаем 12 ГБ, так как WMI может возвращать неправильное значение
                                    if (name.Contains("RTX 3060"))
                                    {
                                        GPUMemoryText.Text = "12.00 ГБ";
                                    }
                                    else
                                    {
                                        GPUMemoryText.Text = $"{vramGB:F2} ГБ";
                                    }
                                    videoMemoryFound = true;
                                }
                                
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Если первый метод не сработал, продолжаем со следующим
                }
                
                // Если не удалось получить через первый метод, пробуем через стандартный запрос
                if (!videoMemoryFound)
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController"))
                    {
                        foreach (ManagementObject gpu in searcher.Get().Cast<ManagementObject>())
                        {
                            // Получаем модель видеокарты
                            string name = gpu["Name"]?.ToString() ?? "Неизвестно";
                            GPUModelText.Text = name;
                            
                            // Если это NVIDIA RTX 3060, явно задаем 12 ГБ
                            if (name.Contains("RTX 3060"))
                            {
                                GPUMemoryText.Text = "12.00 ГБ";
                                videoMemoryFound = true;
                                break;
                            }
                            
                            // Получаем объем видеопамяти
                            if (gpu["AdapterRAM"] != null)
                            {
                                ulong adapterRAM = Convert.ToUInt64(gpu["AdapterRAM"]);
                                double adapterRAMGB = adapterRAM / 1024.0 / 1024.0 / 1024.0;
                                GPUMemoryText.Text = $"{adapterRAMGB:F2} ГБ";
                                videoMemoryFound = true;
                            }
                            
                            break;
                        }
                    }
                }
                
                // Если всё ещё не удалось получить данные, пробуем через реестр Windows
                if (!videoMemoryFound)
                {
                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"))
                        {
                            if (key != null)
                            {
                                // Проверяем наличие информации о видеопамяти в реестре
                                object memorySize = key.GetValue("HardwareInformation.MemorySize");
                                if (memorySize != null)
                                {
                                    ulong memSizeBytes = Convert.ToUInt64(memorySize);
                                    double memSizeGB = memSizeBytes / 1024.0 / 1024.0 / 1024.0;
                                    GPUMemoryText.Text = $"{memSizeGB:F2} ГБ";
                                    videoMemoryFound = true;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки при доступе к реестру
                    }
                }
                
                // Если не удалось получить данные через стандартные методы, явно определяем по модели
                if (!videoMemoryFound)
                {
                    string model = GPUModelText.Text.ToLower();
                    if (model.Contains("rtx 3060"))
                    {
                        GPUMemoryText.Text = "12.00 ГБ";
                    }
                    else if (model.Contains("rtx 3070"))
                    {
                        GPUMemoryText.Text = "8.00 ГБ";
                    }
                    else if (model.Contains("rtx 3080"))
                    {
                        GPUMemoryText.Text = "10.00 ГБ";
                    }
                    else if (model.Contains("rtx 3090"))
                    {
                        GPUMemoryText.Text = "24.00 ГБ";
                    }
                    else
                    {
                        GPUMemoryText.Text = "Не удалось определить";
                    }
                }
                
                // Получаем информацию о разрешении экрана
                ScreenResolutionText.Text = $"{SystemParameters.PrimaryScreenWidth} x {SystemParameters.PrimaryScreenHeight}";
                
                // Получаем информацию о частоте обновления экрана
                try
                {
                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source != null)
                    {
                        CompositionTarget presentationTarget = source.CompositionTarget;
                        double refreshRate = 60; // Значение по умолчанию
                        
                        // Проверяем, доступно ли свойство для получения частоты
                        var dpiProperty = typeof(CompositionTarget).GetProperty("RefreshRate");
                        if (dpiProperty != null)
                        {
                            refreshRate = (double)dpiProperty.GetValue(presentationTarget);
                        }
                        else
                        {
                            // Пробуем получить через WMI
                            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CurrentRefreshRate FROM Win32_VideoController"))
                            {
                                foreach (ManagementObject gpu in searcher.Get().Cast<ManagementObject>())
                                {
                                    if (gpu["CurrentRefreshRate"] != null)
                                    {
                                        refreshRate = Convert.ToDouble(gpu["CurrentRefreshRate"]);
                                        break;
                                    }
                                }
                            }
                        }
                        
                        RefreshRateText.Text = $"{refreshRate:F0} Гц";
                    }
                    else
                    {
                        RefreshRateText.Text = "Неизвестно";
                    }
                }
                catch
                {
                    RefreshRateText.Text = "Неизвестно";
                }
            }
            catch (Exception ex)
            {
                GPUModelText.Text = $"Ошибка получения данных: {ex.Message}";
                GPUMemoryText.Text = "Ошибка получения данных";
                ScreenResolutionText.Text = "Ошибка получения данных";
                RefreshRateText.Text = "Ошибка получения данных";
            }
        }
        
        /// <summary>
        /// Получение информации о сетевых адаптерах
        /// </summary>
        private void GetNetworkInfo()
        {
            try
            {
                _networkAdapters.Clear();
                
                // Получаем все сетевые интерфейсы
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                if (interfaces.Length == 0)
                {
                    NetworkAdaptersLoadingText.Text = "Сетевые адаптеры не обнаружены";
                    return;
                }
                
                // Фильтруем и отображаем только активные адаптеры
                var activeInterfaces = interfaces.Where(i => 
                    i.OperationalStatus == OperationalStatus.Up && 
                    (i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || 
                     i.NetworkInterfaceType == NetworkInterfaceType.Ethernet)).ToList();
                
                foreach (NetworkInterface ni in activeInterfaces)
                {
                    IPInterfaceProperties ipProps = ni.GetIPProperties();
                    
                    // Добавляем имя адаптера
                    _networkAdapters.Add(new NetworkAdapterInfo { 
                        Name = "Имя адаптера", 
                        Value = ni.Name 
                    });
                    
                    // Добавляем тип адаптера
                    _networkAdapters.Add(new NetworkAdapterInfo { 
                        Name = "Тип подключения", 
                        Value = GetInterfaceTypeDescription(ni.NetworkInterfaceType) 
                    });
                    
                    // Добавляем скорость соединения
                    _networkAdapters.Add(new NetworkAdapterInfo { 
                        Name = "Скорость", 
                        Value = $"{ni.Speed / 1000000} Мбит/с" 
                    });
                    
                    // Добавляем IP адреса
                    var ipAddresses = ipProps.UnicastAddresses
                        .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(ip => ip.Address.ToString())
                        .ToList();
                    
                    if (ipAddresses.Any())
                    {
                        _networkAdapters.Add(new NetworkAdapterInfo { 
                            Name = "IP адрес", 
                            Value = string.Join(", ", ipAddresses) 
                        });
                    }
                    
                    // Добавляем разделитель между адаптерами
                    if (activeInterfaces.IndexOf(ni) < activeInterfaces.Count - 1)
                    {
                        _networkAdapters.Add(new NetworkAdapterInfo { 
                            Name = "---", 
                            Value = "---" 
                        });
                    }
                }
                
                // Обновляем отображение в ItemsControl
                NetworkAdaptersItemsControl.ItemsSource = _networkAdapters;
                
                // Скрываем текст загрузки
                NetworkAdaptersLoadingText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                NetworkAdaptersLoadingText.Text = $"Ошибка получения данных о сетевых адаптерах: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Преобразует тип сетевого интерфейса в понятное описание
        /// </summary>
        private string GetInterfaceTypeDescription(NetworkInterfaceType type)
        {
            switch (type)
            {
                case NetworkInterfaceType.Wireless80211:
                    return "Wi-Fi";
                case NetworkInterfaceType.Ethernet:
                    return "Ethernet";
                case NetworkInterfaceType.Loopback:
                    return "Loopback";
                case NetworkInterfaceType.Ppp:
                    return "PPP";
                default:
                    return type.ToString();
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку автоматической оптимизации
        /// </summary>
        private void AutoOptimizationButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Реализовать автоматическую оптимизацию
            
            // Пример обновления статуса
            UpdateOptimizationStatus("Выполняется автоматическая оптимизация...", StatusType.InProgress);
            
            // Показываем экран загрузки
            ShowLoadingScreen("Выполняется автоматическая оптимизация...");
            
            // Имитация процесса оптимизации
            Task.Run(async () => 
            {
                // Имитируем процесс, который занимает некоторое время
                await Task.Delay(3000);
                
                // Обновляем UI в потоке UI
                Dispatcher.Invoke(() => 
                {
                    // Скрываем экран загрузки
                    HideLoadingScreen();
                    
                    // Обновляем статус оптимизации
                    UpdateOptimizationStatus("Автоматическая оптимизация успешно выполнена!", StatusType.Success);
                });
            });
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку мануальной оптимизации
        /// </summary>
        private void ManualOptimizationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем статус
                UpdateOptimizationStatus("Запускается мануальная оптимизация...", StatusType.InProgress);
                
                // Открываем окно мануальной оптимизации
                var manualOptimizationWindow = new ManualOptimizationWindow();
                manualOptimizationWindow.Owner = this;
                
                try
                {
                    // Показываем окно мануальной оптимизации
                    bool? result = manualOptimizationWindow.ShowDialog();
                    
                    if (result == true)
                    {
                        // Если оптимизация была выполнена, обновляем статус
                        UpdateOptimizationStatus("Мануальная оптимизация успешно выполнена!", StatusType.Success);
                    }
                    else
                    {
                        // Если пользователь отменил оптимизацию
                        UpdateOptimizationStatus("Мануальная оптимизация была отменена", StatusType.Warning);
                    }
                }
                catch (Exception dialogEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при отображении диалога: {dialogEx.Message}");
                    UpdateOptimizationStatus("Ошибка при выполнении мануальной оптимизации", StatusType.Error);
                    MessageBox.Show($"Произошла ошибка: {dialogEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Критическая ошибка в обработчике кнопки мануальной оптимизации: {ex.Message}");
                UpdateOptimizationStatus("Критическая ошибка при запуске мануальной оптимизации", StatusType.Error);
                MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Тип статуса оптимизации
        /// </summary>
        private enum StatusType
        {
            None,
            Success,
            Warning,
            Error,
            InProgress
        }
        
        /// <summary>
        /// Обновляет статус оптимизации
        /// </summary>
        private void UpdateOptimizationStatus(string message, StatusType statusType)
        {
            try
            {
                if (OptimizationStatusText != null)
                    OptimizationStatusText.Text = message;
                
                // Обновляем иконку в зависимости от типа статуса
                if (StatusIcon != null)
                {
                    switch (statusType)
                    {
                        case StatusType.Success:
                            StatusIcon.Text = "\uE930"; // Галочка
                            StatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6e3a1"));
                            break;
                        case StatusType.Warning:
                            StatusIcon.Text = "\uE7BA"; // Восклицательный знак
                            StatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9e2af"));
                            break;
                        case StatusType.Error:
                            StatusIcon.Text = "\uE783"; // Крестик
                            StatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f38ba8"));
                            break;
                        case StatusType.InProgress:
                            StatusIcon.Text = "\uE895"; // Круговая стрелка
                            StatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89b4fa"));
                            break;
                        default:
                            StatusIcon.Text = "\uE946"; // Информация
                            StatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cdd6f4"));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении статуса: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Настраивает анимации индикатора загрузки
        /// </summary>
        private void SetupLoadingAnimations()
        {
            // Настройка анимации вращения
            _loadingRotationStoryboard = new Storyboard();
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1.5),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(rotateAnimation, LoadingRotation);
            Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath("Angle"));
            _loadingRotationStoryboard.Children.Add(rotateAnimation);
            
            // Настройка анимации точек
            _dotsAnimationTimer.Interval = TimeSpan.FromMilliseconds(350);
            _dotsAnimationTimer.Tick += (s, e) => 
            {
                // Сбрасываем прозрачность всех точек
                Dot1.Opacity = 0.4;
                Dot2.Opacity = 0.4;
                Dot3.Opacity = 0.4;
                
                // Подсвечиваем текущую точку
                switch (_dotAnimationStep % 3)
                {
                    case 0:
                        Dot1.Opacity = 1.0;
                        break;
                    case 1:
                        Dot2.Opacity = 1.0;
                        break;
                    case 2:
                        Dot3.Opacity = 1.0;
                        break;
                }
                
                _dotAnimationStep++;
            };
        }
        
        /// <summary>
        /// Показывает экран загрузки с указанным сообщением
        /// </summary>
        private void ShowLoadingScreen(string message = "Загрузка...")
        {
            // Устанавливаем текст загрузки
            LoadingText.Text = message;
            
            // Показываем панель загрузки
            LoadingPanel.Visibility = Visibility.Visible;
            
            // Запускаем анимацию вращения
            if (_loadingRotationStoryboard == null)
            {
                // Создаем анимацию вращения
                var rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(2),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                
                // Создаем раскадровку
                _loadingRotationStoryboard = new Storyboard();
                Storyboard.SetTarget(rotateAnimation, LoadingRotation);
                Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath(RotateTransform.AngleProperty));
                _loadingRotationStoryboard.Children.Add(rotateAnimation);
            }
            
            // Запускаем раскадровку
            _loadingRotationStoryboard.Begin();
            
            // Запускаем анимацию точек
            _dotsAnimationTimer.Start();
        }
        
        /// <summary>
        /// Скрывает экран загрузки
        /// </summary>
        private void HideLoadingScreen()
        {
            // Скрываем панель загрузки
            LoadingPanel.Visibility = Visibility.Collapsed;
            
            // Останавливаем анимацию вращения
            _loadingRotationStoryboard?.Stop();
            
            // Останавливаем анимацию точек
            _dotsAnimationTimer.Stop();
        }
        
        /// <summary>
        /// Обработчик таймера для анимации точек
        /// </summary>
        private void DotsAnimationTimer_Tick(object sender, EventArgs e)
        {
            // Меняем прозрачность точек в зависимости от текущего шага анимации
            Dot1.Opacity = _dotAnimationStep == 0 ? 1.0 : 0.3;
            Dot2.Opacity = _dotAnimationStep == 1 ? 1.0 : 0.3;
            Dot3.Opacity = _dotAnimationStep == 2 ? 1.0 : 0.3;
            
            // Переходим к следующему шагу анимации
            _dotAnimationStep = (_dotAnimationStep + 1) % 3;
        }

        // Для перетаскивания окна
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
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
        private void OptimizationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Останавливаем таймеры перед закрытием
            _dotsAnimationTimer.Stop();
            _systemInfoUpdateTimer.Stop();
            
            // Если нужно, открываем главное окно
            if (_openMainWindowOnClose)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
        
        // Обработчик нажатия кнопки "Закрыть"
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Останавливаем таймеры перед закрытием
            _dotsAnimationTimer.Stop();
            _systemInfoUpdateTimer.Stop();
            
            this.Close();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку полноэкранного режима
        /// </summary>
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isFullScreen)
            {
                // Сохраняем текущие настройки окна
                _originalWidth = Width;
                _originalHeight = Height;
                _originalWindowState = WindowState;
                _originalWindowStyle = WindowStyle;
                _originalResizeMode = ResizeMode;
                _originalAllowsTransparency = AllowsTransparency;
                _originalBorderThickness = BorderThickness;
                
                // Переключаем в полноэкранный режим
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                FullScreenButton.ToolTip = "Выйти из полноэкранного режима";
                
                // Обновляем иконку кнопки
                var textBlock = FullScreenButton.Content as Grid;
                if (textBlock != null && textBlock.Children.Count > 0 && textBlock.Children[0] is TextBlock tb)
                {
                    tb.Text = "\uE1D8"; // Иконка выхода из полноэкранного режима
                }
            }
            else
            {
                // Восстанавливаем исходные настройки
                Width = _originalWidth;
                Height = _originalHeight;
                WindowState = _originalWindowState;
                WindowStyle = _originalWindowStyle;
                ResizeMode = _originalResizeMode;
                AllowsTransparency = _originalAllowsTransparency;
                BorderThickness = _originalBorderThickness;
                FullScreenButton.ToolTip = "Полноэкранный режим";
                
                // Восстанавливаем иконку кнопки
                var textBlock = FullScreenButton.Content as Grid;
                if (textBlock != null && textBlock.Children.Count > 0 && textBlock.Children[0] is TextBlock tb)
                {
                    tb.Text = "\uE1D9"; // Иконка перехода в полноэкранный режим
                }
            }
            
            // Инвертируем флаг полноэкранного режима
            _isFullScreen = !_isFullScreen;
        }

        /// <summary>
        /// Обработчик нажатия на кнопку обновления данных
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Добавляем анимацию вращения к кнопке
            var rotateTransform = new RotateTransform();
            RefreshButton.RenderTransform = rotateTransform;
            
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(1)
            };
            
            rotateTransform.CenterX = RefreshButton.Width / 2;
            rotateTransform.CenterY = RefreshButton.Height / 2;
            
            animation.Completed += (s, args) => 
            {
                // Сбрасываем трансформацию после завершения анимации
                RefreshButton.RenderTransform = null;
            };
            
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            
            // Обновляем системную информацию
            GetSystemInfo();
            UpdateLastUpdateTime();
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
    }
} 