using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using App.Helpers;

namespace App.Views
{
    /// <summary>
    /// Простой конвертер для прогресс-бара
    /// </summary>
    public class SimpleProgressConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return 0;
                
            double percent = (double)values[0];
            double width = (double)values[1];
            
            return width * (percent / 100.0);
        }
        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Класс модели диска для отображения в интерфейсе
    /// </summary>
    public class DiskModel
    {
        // Основная информация о диске
        public string DriveLetter { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Диск";
        public string FileSystem { get; set; } = "";
        
        // SVG-путь для иконки диска
        public string IconPath { get; set; } = "M9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4zm2 2H5V5h14v14zm0-16H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z";
        
        // Размеры диска
        public string TotalSpace { get; set; } = "0 ГБ";
        public string UsedSpace { get; set; } = "0 ГБ";
        public string FreeSpace { get; set; } = "0 ГБ";
        public double UsagePercent { get; set; } = 0;
        
        // Дополнительная информация
        public string SpeedInfo { get; set; } = "Н/Д";
        
        // Цвет типа диска в формате строки #RRGGBB
        public string TypeColorHex { get; set; } = "#70A1FF";
        
        // Цвет для прогрессбара заполненности
        public string UsageColorHex 
        { 
            get 
            {
                if (UsagePercent >= 90)
                    return "#F38BA8"; // Красный - критическое заполнение
                else if (UsagePercent >= 70)
                    return "#FAB387"; // Оранжевый - высокое заполнение
                else
                    return "#7BED9F"; // Зеленый - нормальное заполнение
            }
        }
        
        // Возвращает соответствующую иконку в зависимости от типа диска
        public void SetupIconForDriveType(DriveType driveType, string driveName = "")
        {
            switch (driveType)
            {
                case DriveType.Fixed:
                    // Пытаемся определить тип диска по имени
                    string driveNameUpper = driveName.ToUpper().Trim();
                    
                    if (driveNameUpper.Contains("NVME") || driveNameUpper.Contains("PCIE"))
                    {
                        Type = "NVMe";
                        // Иконка NVMe диска
                        IconPath = "M9,2V4H7V8H5V4H3V2H9M2,9V15H9V9H2M19,9V15H22V9H19M12,9V15H17V9H12M4,11H7V13H4V11M14,11H15V13H14V11M9,16V22H16V16H9M11,18H14V20H11V18Z";
                        TypeColorHex = "#89DCEB";
                    }
                    else if (driveNameUpper.Contains("SSD"))
                    {
                        Type = "SSD";
                        // Иконка SSD диска
                        IconPath = "M4,6H20V16H4V6M4,16H8V18H4V16M12,16H16V18H12V16M4,4H8V6H4V4M4,18H8V20H4V18M12,18H16V20H12V18M10,16H12V18H10V16M12,4H16V6H12V4M10,4H12V6H10V4Z";
                        TypeColorHex = "#70A1FF";
                    }
                    else
                    {
                        Type = "HDD";
                        // Иконка HDD диска
                        IconPath = "M6,2H18A2,2 0 0,1 20,4V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V4A2,2 0 0,1 6,2M6,4V20H18V4H6M16,9H14V6H16V9M12,9H10V6H12V9M8,9H6V6H8V9M16,12H14V9H16V12M12,12H10V9H12V12M8,12H6V9H8V12M16,15H14V12H16V15M12,15H10V12H12V15M8,15H6V12H8V15M16,18H14V15H16V18M12,18H10V15H12V18M8,18H6V15H8V18Z";
                        TypeColorHex = "#A9B1D6";
                    }
                    break;
                    
                case DriveType.Removable:
                    Type = "USB";
                    // Иконка USB-флешки
                    IconPath = "M12 14V6c0-1.1.9-2 2-2h6c1.1 0 2 .9 2 2v14h2v2H2v-2h2V9c0-1.1.9-2 2-2h4v7H12zM6 9v11h12V9H6z";
                    TypeColorHex = "#FAB387";
                    break;
                    
                case DriveType.Network:
                    Type = "Сетевой";
                    // Иконка сетевого диска
                    IconPath = "M4,5H20V7H4V5M4,9H20V11H4V9M4,13H20V15H4V13M4,17H14V19H4V17Z";
                    TypeColorHex = "#CAA9FE";
                    break;
                    
                case DriveType.CDRom:
                    Type = "CD/DVD";
                    // Иконка оптического диска
                    IconPath = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,9A3,3 0 0,1 15,12A3,3 0 0,1 12,15A3,3 0 0,1 9,12A3,3 0 0,1 12,9Z";
                    TypeColorHex = "#F38BA8";
                    break;
                    
                default:
                    Type = "Диск";
                    // Иконка по умолчанию
                    IconPath = "M6,2H18A2,2 0 0,1 20,4V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V4A2,2 0 0,1 6,2M12,4A6,6 0 0,0 6,10C6,13.31 8.69,16 12.1,16L11.22,13.77C10.95,13.29 11.11,12.68 11.59,12.4L12.45,11.9C12.93,11.63 13.54,11.79 13.82,12.27L15.74,14.69C17.12,13.59 18,11.9 18,10A6,6 0 0,0 12,4M12,9A1,1 0 0,1 13,10A1,1 0 0,1 12,11A1,1 0 0,1 11,10A1,1 0 0,1 12,9Z";
                    TypeColorHex = "#9CA0B0";
                    break;
            }
        }
    }
    
    /// <summary>
    /// Логика взаимодействия для DiskInfoWindow.xaml
    /// </summary>
    public partial class DiskInfoWindow : Window
    {
        private List<DiskModel> _disks = new List<DiskModel>();
        private Storyboard _loadingStoryboard = new Storyboard();
        private bool _isLoading = false;
        private bool _openMainWindowOnClose = true;
        
        public DiskInfoWindow()
        {
            try
            {
                InitializeComponent();
                
                // Центрируем окно относительно активного монитора
                this.Loaded += DiskInfoWindow_Loaded;
                
                // Настройка анимации загрузки
                var rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(1.5),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(rotateAnimation, LoadingRotation);
                Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath("Angle"));
                _loadingStoryboard.Children.Add(rotateAnimation);
                
                // Установим обработчик необработанных исключений
                AppDomain.CurrentDomain.UnhandledException += (s, e) => 
                {
                    var ex = e.ExceptionObject as Exception;
                    MessageBox.Show($"Критическая ошибка: {ex?.Message}\n\nStack Trace: {ex?.StackTrace}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                };
                
                // Загрузка информации о дисках
                LoadDisksInfo();
                
                // Добавляем обработчик закрытия окна
                Closing += DiskInfoWindow_Closing;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации окна информации о дисках: {ex.Message}\n\nStack Trace: {ex.StackTrace}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Обработчик события загрузки окна
        /// </summary>
        private void DiskInfoWindow_Loaded(object sender, RoutedEventArgs e)
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
        /// Определяет тип диска (SSD/NVMe/HDD) по букве диска через прямое сопоставление
        /// </summary>
        private string DetermineDriveType(string driveLetter)
        {
            try
            {
                // Очищаем букву диска для запросов
                string driveLetterClean = driveLetter.Replace("\\", "").Replace(":", "");
                string initialResult = null;
                
                // Проверяем наличие явных признаков SSD/HDD
                bool hasSpindleSpeed = false;
                bool hasRotational = false;
                string spindleModel = "";
                
                // Метод 1: Проверяем наличие признаков вращающегося диска (HDD)
                try
                {
                    using (var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{driveLetterClean}:'"))
                    {
                        foreach (ManagementObject logicalDisk in searcher.Get())
                        {
                            foreach (ManagementObject partition in logicalDisk.GetRelated("Win32_DiskPartition"))
                            {
                                foreach (ManagementObject physicalDisk in partition.GetRelated("Win32_DiskDrive"))
                                {
                                    // Проверяем модель - возможно есть там ключевые слова
                                    if (physicalDisk["Model"] != null)
                                    {
                                        spindleModel = physicalDisk["Model"].ToString();
                                        string forcedType = ForceDiskTypeByModel(spindleModel);
                                        if (!string.IsNullOrEmpty(forcedType))
                                        {
                                            Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как {forcedType} по модели {spindleModel}");
                                            return forcedType;
                                        }
                                        
                                        // Ищем "шпиндель" - прямой признак HDD
                                        if (spindleModel.ToLower().Contains("spindle") || 
                                            spindleModel.ToLower().Contains("spinpoint"))
                                        {
                                            Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как HDD (содержит keyword spindle в модели)");
                                            return "HDD";
                                        }
                                    }
                                    
                                    // Проверяем наличие скорости шпинделя
                                    if (physicalDisk["Size"] != null && physicalDisk["Signature"] != null)
                                    {
                                        try
                                        {
                                            // Проверяем для HDD
                                            var query = $"SELECT * FROM Win32_DiskDrive WHERE DeviceID='{physicalDisk["DeviceID"]}'";
                                            using (var searcher2 = new ManagementObjectSearcher(query))
                                            {
                                                foreach (ManagementObject disk in searcher2.Get())
                                                {
                                                    if (disk["Caption"] != null)
                                                    {
                                                        string caption = disk["Caption"].ToString().ToUpper();
                                                        if (caption.Contains("ATA") && (caption.Contains("ST") || caption.Contains("WDC") || 
                                                                                      caption.Contains("HITACHI") || caption.Contains("TOSHIBA")))
                                                        {
                                                            Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как HDD по SCSI/ATA");
                                                            return "HDD";
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при определении типа через Win32_LogicalDisk: {ex.Message}");
                }
                
                // Если первый метод не помог, пытаемся через Storage API
                try
                {
                    ManagementScope scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                    scope.Connect();
                    
                    ObjectQuery query = new ObjectQuery(
                        $"SELECT * FROM MSFT_Partition WHERE DriveLetter='{driveLetterClean}'");
                    
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                    {
                        foreach (ManagementObject partition in searcher.Get())
                        {
                            if (partition["DiskNumber"] != null)
                            {
                                uint diskNumber = Convert.ToUInt32(partition["DiskNumber"]);
                                
                                query = new ObjectQuery($"SELECT * FROM MSFT_PhysicalDisk WHERE DeviceId='{diskNumber}'");
                                using (ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher(scope, query))
                                {
                                    foreach (ManagementObject physicalDisk in diskSearcher.Get())
                                    {
                                        // Проверяем SpindleSpeed - если > 0, то это HDD
                                        if (physicalDisk["SpindleSpeed"] != null)
                                        {
                                            hasSpindleSpeed = true;
                                            uint spindleSpeed = Convert.ToUInt32(physicalDisk["SpindleSpeed"]);
                                            if (spindleSpeed > 0)
                                            {
                                                Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как HDD (SpindleSpeed={spindleSpeed})");
                                                return "HDD";
                                            }
                                        }
                                        
                                        // На современных Windows есть прямой параметр SSD
                                        if (physicalDisk["MediaType"] != null)
                                        {
                                            uint mediaType = Convert.ToUInt32(physicalDisk["MediaType"]);
                                            if (mediaType == 3 || mediaType == 4)
                                            {
                                                Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как SSD (MediaType={mediaType})");
                                                return "SSD";
                                            }
                                            else if (mediaType == 0 || mediaType == 1)
                                            {
                                                Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как HDD (MediaType={mediaType})");
                                                return "HDD";
                                            }
                                        }
                                        
                                        // Проверяем, является ли диск SSD по свойству RotationalMediaType
                                        if (physicalDisk["RotationSpeed"] != null)
                                        {
                                            hasRotational = true;
                                            uint rotationSpeed = Convert.ToUInt32(physicalDisk["RotationSpeed"]);
                                            if (rotationSpeed > 0)
                                            {
                                                Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как HDD (RotationSpeed={rotationSpeed})");
                                                return "HDD";
                                            }
                                            else if (rotationSpeed == 0)
                                            {
                                                Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как SSD (RotationSpeed=0)");
                                                initialResult = "SSD"; // Пометим как SSD, но продолжим проверки
                                            }
                                        }
                                        
                                        // Проверяем BusType (17 = NVMe)
                                        if (physicalDisk["BusType"] != null)
                                        {
                                            uint busType = Convert.ToUInt32(physicalDisk["BusType"]);
                                            if (busType == 17)
                                            {
                                                Debug.WriteLine($"WMI распознавание: диск {driveLetter} определен как NVMe (BusType={busType})");
                                                return "NVMe";
                                            }
                                            
                                            // Если это один из типов SATA/IDE - проверим другие признаки HDD
                                            if ((busType == 3 || busType == 1 || busType == 8 || busType == 11) && 
                                               (hasSpindleSpeed || hasRotational))
                                            {
                                                Debug.WriteLine($"WMI распознавание: диск {driveLetter} вероятно HDD (BusType={busType})");
                                                if (initialResult == null) initialResult = "HDD"; 
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при определении типа через Storage API: {ex.Message}");
                }
                
                // Возвращаем вероятный результат или "по умолчанию" HDD для локальных дисков
                return initialResult ?? "HDD";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Общая ошибка при определении типа диска: {ex.Message}");
                return "Диск";
            }
        }
        
        /// <summary>
        /// Принудительно определяет тип диска по его модели/имени
        /// </summary>
        private string ForceDiskTypeByModel(string model)
        {
            if (string.IsNullOrEmpty(model))
                return null;
            
            string upperModel = model.ToUpper();
            
            // Известные NVMe диски
            string[] nvmeKeywords = {
                "NVME", "PCIE", "PCIEx", "M.2", "970 EVO", "980 PRO", "SN850", "MP600", 
                "FORCE MP", "AORUS", "SX8200", "KC3000", "P5 PLUS", "660P", "665P",
                "FIRECUDA", "BLACK SN", "WD_BLACK", "XPG", "CORSAIR FORCE", "KINGSTON FURY"
            };
            
            foreach (var keyword in nvmeKeywords)
            {
                if (upperModel.Contains(keyword))
                    return "NVMe";
            }
            
            // Известные SSD диски - строгое соответствие
            string[] ssdKeywords = {
                "SSD", "SOLID STATE", "860 EVO", "870 EVO", "MX500", "BX500", "CRUCIAL", 
                "KINGSTON A", "KINGSTON KC", "SAMSUNG EVO", "SAMSUNG QVO", "INTEL SSD", 
                "ADATA SU", "SANDISK PLUS", "SANDISK ULTRA", "KINGSTON UV"
            };
            
            foreach (var keyword in ssdKeywords)
            {
                if (upperModel.Contains(keyword))
                {
                    // Проверяем, что это не ложное срабатывание HDD с SSD словом в названии
                    if (upperModel.Contains("HDD") && !upperModel.Contains("SSD HDD") && 
                        !upperModel.Contains("SSHD"))
                    {
                        continue; // Пропускаем, если это HDD с SSD в названии
                    }
                    return "SSD";
                }
            }
            
            // Известные HDD диски - добавляем больше ключевых слов и более агрессивное определение
            string[] hddKeywords = {
                "HDD", "HARD DRIVE", "HARD DISK", "BARRACUDA", "WD BLUE", "WD RED", "WD BLACK", 
                "TOSHIBA P300", "SEAGATE", "IRONWOLF", "SKYHAWK", "CAVIAR", "ST3", "ST4", "ST1", "ST2",
                "WDC WD", "DT01ACA", "HDW", "EFRX", "EXOS", "TOSHIBA", "HITACHI", "HGST", 
                "WD10EZEX", "WD20EZRZ", "WD30EZRZ", "WD40EZRZ", "ST1000DM", "ST2000DM", "ST3000DM",
                "HD", "DESKTOP HDD"
            };
            
            foreach (var keyword in hddKeywords)
            {
                if (upperModel.Contains(keyword))
                    return "HDD";
            }
            
            // Если у диска очень большой размер (более 2TB) и нет признаков SSD - вероятно это HDD
            if (upperModel.Contains("2TB") || upperModel.Contains("3TB") || 
                upperModel.Contains("4TB") || upperModel.Contains("6TB") ||
                upperModel.Contains("8TB") || upperModel.Contains("10TB") || 
                upperModel.Contains("12TB") || upperModel.Contains("14TB") || 
                upperModel.Contains("16TB") || upperModel.Contains("18TB") || 
                upperModel.Contains("20TB"))
            {
                return "HDD";
            }
            
            return null; // Не удалось определить
        }
        
        /// <summary>
        /// Загружает информацию о дисках асинхронно
        /// </summary>
        private void LoadDisksInfo()
        {
            try
            {
                // Показываем индикатор загрузки
                _isLoading = true;
                LoadingPanel.Visibility = Visibility.Visible;
                try 
                { 
                    _loadingStoryboard?.Begin(); 
                } 
                catch (Exception ex) 
                {
                    Debug.WriteLine($"Ошибка анимации: {ex.Message}");
                }
                
                // Запускаем асинхронную загрузку в отдельном потоке
                Task.Run(() => 
                {
                    // Временный список для сбора информации
                    var tempDisks = new List<DiskModel>();
                    
                    try
                    {
                        DriveInfo[] drives;
                        
                        try 
                        {
                            drives = DriveInfo.GetDrives();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка при получении списка дисков: {ex.Message}");
                            drives = new DriveInfo[0];
                            
                            // Обновляем UI в главном потоке
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                MessageBox.Show($"Ошибка при получении списка дисков: {ex.Message}", 
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                        
                        foreach (DriveInfo drive in drives)
                        {
                            try
                            {
                                // Создаем модель диска с упрощенной информацией
                                var diskModel = new DiskModel
                                {
                                    DriveLetter = drive.Name,
                                    Name = "Диск"
                                };
                                
                                // Определяем тип диска - упрощаем логику для ускорения
                                string diskType = DetermineSimpleDriveType(drive);
                                
                                // Устанавливаем иконку на основе типа диска
                                SetupDiskModelFromType(diskModel, diskType, drive.DriveType);
                                
                                // Дополнительная информация
                                diskModel.FileSystem = drive.IsReady ? drive.DriveFormat : "Не доступно";
                                
                                if (drive.IsReady)
                                {
                                    try
                                    {
                                        // Получаем объемы с минимальными вычислениями
                                        diskModel.TotalSpace = FormatByteSize(drive.TotalSize);
                                        diskModel.FreeSpace = FormatByteSize(drive.AvailableFreeSpace);
                                        diskModel.UsedSpace = FormatByteSize(drive.TotalSize - drive.AvailableFreeSpace);
                                        
                                        // Вычисляем заполненность в процентах
                                        double totalGB = (double)drive.TotalSize / (1024 * 1024 * 1024);
                                        double usedGB = (double)(drive.TotalSize - drive.AvailableFreeSpace) / (1024 * 1024 * 1024);
                                        diskModel.UsagePercent = Math.Round((usedGB / totalGB) * 100, 0);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Ошибка при расчете размера диска {drive.Name}: {ex.Message}");
                                        
                                        diskModel.TotalSpace = "Ошибка";
                                        diskModel.FreeSpace = "Ошибка";
                                        diskModel.UsedSpace = "Ошибка";
                                    }
                                }
                                else
                                {
                                    diskModel.TotalSpace = "Не доступно";
                                    diskModel.FreeSpace = "Не доступно";
                                    diskModel.UsedSpace = "Не доступно";
                                }
                                
                                // Добавляем диск в коллекцию
                                tempDisks.Add(diskModel);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Ошибка при обработке диска {drive.Name}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Общая ошибка при загрузке дисков: {ex.Message}");
                    }
                    
                    // Обновляем UI в главном потоке
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        try 
                        {
                            // Очищаем старые данные
                            _disks.Clear();
                            
                            // Копируем данные из временного списка
                            _disks.AddRange(tempDisks);
                            
                            // Обновляем источник данных
                            DisksList.ItemsSource = null;
                            DisksList.ItemsSource = _disks;
                            
                            // Скрываем индикатор загрузки
                            _isLoading = false;
                            LoadingPanel.Visibility = Visibility.Collapsed;
                            _loadingStoryboard?.Stop();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка при обновлении UI: {ex.Message}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Критическая ошибка: {ex.Message}");
                MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Скрываем индикатор загрузки в случае ошибки
                _isLoading = false;
                LoadingPanel.Visibility = Visibility.Collapsed;
                _loadingStoryboard?.Stop();
            }
        }
        
        /// <summary>
        /// Форматирует размер в байтах в читаемый формат
        /// </summary>
        private string FormatByteSize(long byteSize)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            double len = byteSize;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1) 
            {
                order++;
                len = len / 1024;
            }
            
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }
        
        /// <summary>
        /// Быстрое определение типа диска без использования WMI
        /// </summary>
        private string DetermineSimpleDriveType(DriveInfo drive)
        {
            // Если это не фиксированный диск, возвращаем тип как есть
            if (drive.DriveType != DriveType.Fixed)
                return drive.DriveType.ToString();
                
            // Для фиксированных дисков используем простую эвристику
            // По умолчанию считаем диск HDD
            string diskType = "HDD";
                
            try
            {
                // Проверяем системный диск - он обычно SSD в современных системах
                if (drive.Name.StartsWith(Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    diskType = "SSD";
                }
                
                // Для более точного определения можно проверить производительность
                // Но это может быть медленной операцией
                
                return diskType;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в DetermineSimpleDriveType: {ex.Message}");
                return "HDD"; // По умолчанию
            }
        }
        
        /// <summary>
        /// Устанавливает свойства модели диска на основе его типа
        /// </summary>
        private void SetupDiskModelFromType(DiskModel model, string diskType, DriveType driveType)
        {
            if (driveType == DriveType.Fixed)
            {
                if (diskType == "NVMe")
                {
                    model.Type = "NVMe";
                    model.IconPath = "M9,2V4H7V8H5V4H3V2H9M2,9V15H9V9H2M19,9V15H22V9H19M12,9V15H17V9H12M4,11H7V13H4V11M14,11H15V13H14V11M9,16V22H16V16H9M11,18H14V20H11V18Z";
                    model.TypeColorHex = "#89DCEB";
                }
                else if (diskType == "SSD")
                {
                    model.Type = "SSD";
                    model.IconPath = "M4,6H20V16H4V6M4,16H8V18H4V16M12,16H16V18H12V16M4,4H8V6H4V4M4,18H8V20H4V18M12,18H16V20H12V18M10,16H12V18H10V16M12,4H16V6H12V4M10,4H12V6H10V4Z";
                    model.TypeColorHex = "#70A1FF";
                }
                else
                {
                    model.Type = "HDD";
                    model.IconPath = "M6,2H18A2,2 0 0,1 20,4V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V4A2,2 0 0,1 6,2M6,4V20H18V4H6M16,9H14V6H16V9M12,9H10V6H12V9M8,9H6V6H8V9M16,12H14V9H16V12M12,12H10V9H12V12M8,12H6V9H8V12M16,15H14V12H16V15M12,15H10V12H12V15M8,15H6V12H8V15M16,18H14V15H16V18M12,18H10V15H12V18M8,18H6V15H8V18Z";
                    model.TypeColorHex = "#A9B1D6";
                }
            }
            else
            {
                model.SetupIconForDriveType(driveType, model.DriveLetter);
            }
        }
        
        /// <summary>
        /// Перетаскивание окна
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        
        /// <summary>
        /// Обработчик клика по кнопке обновления
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoading)
            {
                LoadDisksInfo();
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
        private void DiskInfoWindow_Closing(object sender, CancelEventArgs e)
        {
            // Останавливаем таймеры и анимации перед закрытием
            StopAnimations();
            
            // Если нужно, открываем главное окно
            if (_openMainWindowOnClose)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
        
        /// <summary>
        /// Останавливает все анимации перед закрытием окна
        /// </summary>
        private void StopAnimations()
        {
            // Остановка анимации загрузки
            if (_loadingStoryboard != null)
            {
                _loadingStoryboard.Stop();
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку закрытия
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку мануальной очистки (WinDirStat)
        /// </summary>
        private void ManualCleanupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем букву диска из тега кнопки
                if (sender is Button button && button.Tag is string driveLetter)
                {
                    button.IsEnabled = false;
                    
                    // Преобразуем букву диска к формату C:
                    string driveLetterOnly = driveLetter.Replace("\\", "").TrimEnd(':') + ":";
                    
                    try
                    {
                        Debug.WriteLine("Запуск мануальной очистки диска: " + driveLetterOnly);
                        
                        // Получаем путь к встроенной или установленной версии WinDirStat
                        string windirstatPath = GetWinDirStatPath();
                        Debug.WriteLine("Путь к WinDirStat: " + (windirstatPath ?? "не найден"));
                        
                        if (!string.IsNullOrEmpty(windirstatPath))
                        {
                            // Запускаем WinDirStat для выбранного диска
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = windirstatPath,
                                Arguments = $"/dir=\"{driveLetterOnly}\\\"",
                                UseShellExecute = true
                            };
                            
                            Debug.WriteLine($"Запускаем процесс: {windirstatPath} с аргументами: /dir=\"{driveLetterOnly}\\\"");
                            
                            try
                            {
                                Process process = Process.Start(startInfo);
                                if (process != null)
                                {
                                    Debug.WriteLine("Процесс WinDirStat успешно запущен");
                                }
                                else
                                {
                                    Debug.WriteLine("Ошибка: Process.Start вернул null");
                                    MessageBox.Show(
                                        "Не удалось запустить WinDirStat. Пожалуйста, попробуйте запустить программу от имени администратора.",
                                        "Ошибка запуска", 
                                        MessageBoxButton.OK, 
                                        MessageBoxImage.Warning);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Ошибка при запуске процесса WinDirStat: {ex.Message}");
                                MessageBox.Show(
                                    $"Ошибка при запуске WinDirStat: {ex.Message}\nПопробуйте запустить программу от имени администратора.",
                                    "Ошибка запуска", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Warning);
                            }
                        }
                        else
                        {
                            MessageBox.Show(
                                "Не удалось запустить WinDirStat. Внутренняя ошибка извлечения ресурса.",
                                "Ошибка", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка при запуске мануальной очистки: {ex.Message}, StackTrace: {ex.StackTrace}");
                        MessageBox.Show(
                            $"Ошибка при запуске мануальной очистки: {ex.Message}",
                            "Ошибка", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                    
                    button.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Критическая ошибка в обработчике нажатия кнопки мануальной очистки: {ex.Message}, StackTrace: {ex.StackTrace}");
                MessageBox.Show(
                    $"Ошибка в обработчике нажатия кнопки мануальной очистки: {ex.Message}",
                    "Ошибка", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Возвращает путь к WinDirStat (встроенному или установленному)
        /// </summary>
        private string GetWinDirStatPath()
        {
            // Сначала пытаемся извлечь встроенный WinDirStat
            string embeddedPath = ResourceExtractor.ExtractWinDirStat();
            if (!string.IsNullOrEmpty(embeddedPath) && File.Exists(embeddedPath))
            {
                return embeddedPath;
            }
            
            // Если не удалось извлечь встроенный - ищем установленный
            string[] possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinDirStat", "windirstat.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinDirStat", "windirstat.exe")
            };
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            // Если ничего не найдено - сообщаем об ошибке
            MessageBox.Show(
                "Не удалось найти WinDirStat. Возможно, необходимо перезапустить программу с правами администратора.",
                "WinDirStat не найден", 
                MessageBoxButton.OK, 
                MessageBoxImage.Warning);
                
            return null;
        }
    }
    
    /// <summary>
    /// Native методы для работы с корзиной
    /// </summary>
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        internal static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleBinFlags dwFlags);
        
        [Flags]
        internal enum RecycleBinFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }
    }
} 