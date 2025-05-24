using System;
using System.Collections.Generic;
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
        
        public DiskInfoWindow()
        {
            try
            {
                InitializeComponent();
                
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации окна информации о дисках: {ex.Message}\n\nStack Trace: {ex.StackTrace}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Очищаем старые данные
                _disks.Clear();
                
                // Показываем индикатор загрузки
                _isLoading = true;
                LoadingPanel.Visibility = Visibility.Visible;
                try 
                { 
                    _loadingStoryboard?.Begin(); 
                } 
                catch (Exception ex) 
                {
                    MessageBox.Show($"Ошибка анимации: {ex.Message}", "Отладка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                try
                {
                    // Загружаем информацию напрямую в UI-потоке для отладки
                    DriveInfo[] drives;
                    
                    try 
                    {
                        drives = DriveInfo.GetDrives();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при получении списка дисков: {ex.Message}", "Отладка", MessageBoxButton.OK, MessageBoxImage.Information);
                        drives = new DriveInfo[0];
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
                            
                            // Определяем тип диска через WMI
                            string diskType = "HDD";
                            try
                            {
                                if (drive.DriveType == DriveType.Fixed)
                                {
                                    try
                                    {
                                        // Добавляем отладочную информацию
                                        string diskModelInfo = "";
                                        string diskPhysicalId = "";
                                        string diskSize = "";
                                        
                                        try
                                        {
                                            using (var searcher = new ManagementObjectSearcher(
                                                $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID='{drive.Name.Replace("\\", "").Replace(":", "")}:'"))
                                            {
                                                foreach (ManagementObject logicalDisk in searcher.Get())
                                                {
                                                    foreach (ManagementObject partition in logicalDisk.GetRelated("Win32_DiskPartition"))
                                                    {
                                                        foreach (ManagementObject physicalDisk in partition.GetRelated("Win32_DiskDrive"))
                                                        {
                                                            // Сбор отладочной информации
                                                            diskPhysicalId = physicalDisk["DeviceID"]?.ToString() ?? "Unknown";
                                                            diskSize = physicalDisk["Size"]?.ToString() ?? "Unknown";
                                                            
                                                            if (physicalDisk["Model"] != null)
                                                            {
                                                                diskModelInfo = physicalDisk["Model"].ToString();
                                                                Debug.WriteLine($"Отладка диска {drive.Name}: Модель = {diskModelInfo}, Size = {diskSize}, PhysicalID = {diskPhysicalId}");
                                                                
                                                                // Принудительное определение по базе известных моделей
                                                                string forcedType = ForceDiskTypeByModel(diskModelInfo);
                                                                if (!string.IsNullOrEmpty(forcedType))
                                                                {
                                                                    Debug.WriteLine($"Отладка диска {drive.Name}: Принудительно определен как {forcedType} по модели '{diskModelInfo}'");
                                                                    diskType = forcedType;
                                                                    // Если мы принудительно определили тип по модели, прерываем цикл поиска
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Ошибка при получении модели диска {drive.Name}: {ex.Message}");
                                        }
                                        
                                        // Если модель не помогла определить тип - используем стандартный метод
                                        if (diskType == "HDD") // По умолчанию до определения
                                        {
                                            diskType = DetermineDriveType(drive.Name);
                                            Debug.WriteLine($"Отладка диска {drive.Name}: Определен как {diskType} через WMI, модель: {diskModelInfo}, PhysicalID: {diskPhysicalId}");
                                        }
                                        
                                        // Проверим явный HDD признак в модели
                                        if (diskType == "SSD" && !string.IsNullOrEmpty(diskModelInfo) && 
                                            (diskModelInfo.ToUpper().Contains("HDD") || 
                                             diskModelInfo.ToUpper().Contains("HARD DISK") ||
                                             diskModelInfo.ToUpper().Contains("HARD DRIVE")))
                                        {
                                            Debug.WriteLine($"Отладка диска {drive.Name}: Переопределен как HDD из SSD по модели '{diskModelInfo}'");
                                            diskType = "HDD";
                                        }
                                        
                                        // Смотрим на объем - если больше 2TB, скорее это HDD, а не SSD
                                        if (diskType == "SSD" && !string.IsNullOrEmpty(diskSize))
                                        {
                                            try
                                            {
                                                ulong sizeInBytes = Convert.ToUInt64(diskSize);
                                                ulong sizeInTB = sizeInBytes / 1099511627776; // 1TB в байтах
                                                
                                                if (sizeInTB >= 2)
                                                {
                                                    Debug.WriteLine($"Отладка диска {drive.Name}: Возможно переопределение SSD в HDD из-за большого размера {sizeInTB}TB");
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                    catch (Exception ex) 
                                    {
                                        Debug.WriteLine($"Ошибка при определении типа диска {drive.Name}: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    diskType = drive.DriveType.ToString();
                                }
                            }
                            catch (Exception ex) 
                            {
                                Debug.WriteLine($"Ошибка при определении типа диска {drive.Name}: {ex.Message}");
                            }
                            
                            // Устанавливаем иконку на основе типа диска
                            if (drive.DriveType == DriveType.Fixed)
                            {
                                if (diskType == "NVMe")
                                {
                                    diskModel.Type = "NVMe";
                                    diskModel.IconPath = "M9,2V4H7V8H5V4H3V2H9M2,9V15H9V9H2M19,9V15H22V9H19M12,9V15H17V9H12M4,11H7V13H4V11M14,11H15V13H14V11M9,16V22H16V16H9M11,18H14V20H11V18Z";
                                    diskModel.TypeColorHex = "#89DCEB";
                                }
                                else if (diskType == "SSD")
                                {
                                    diskModel.Type = "SSD";
                                    diskModel.IconPath = "M4,6H20V16H4V6M4,16H8V18H4V16M12,16H16V18H12V16M4,4H8V6H4V4M4,18H8V20H4V18M12,18H16V20H12V18M10,16H12V18H10V16M12,4H16V6H12V4M10,4H12V6H10V4Z";
                                    diskModel.TypeColorHex = "#70A1FF";
                                }
                                else
                                {
                                    diskModel.Type = "HDD";
                                    diskModel.IconPath = "M6,2H18A2,2 0 0,1 20,4V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V4A2,2 0 0,1 6,2M6,4V20H18V4H6M16,9H14V6H16V9M12,9H10V6H12V9M8,9H6V6H8V9M16,12H14V9H16V12M12,12H10V9H12V12M8,12H6V9H8V12M16,15H14V12H16V15M12,15H10V12H12V15M8,15H6V12H8V15M16,18H14V15H16V18M12,18H10V15H12V18M8,18H6V15H8V18Z";
                                    diskModel.TypeColorHex = "#A9B1D6";
                                }
                            }
                            else
                            {
                                diskModel.SetupIconForDriveType(drive.DriveType, drive.Name);
                            }
                            
                            // Дополнительная информация
                            diskModel.FileSystem = drive.IsReady ? drive.DriveFormat : "Не доступно";
                            
                            if (drive.IsReady)
                            {
                                try
                                {
                                    // Получаем объемы с минимальными вычислениями
                                    diskModel.TotalSpace = (drive.TotalSize / (1024 * 1024 * 1024)) + " ГБ";
                                    diskModel.FreeSpace = (drive.AvailableFreeSpace / (1024 * 1024 * 1024)) + " ГБ";
                                    diskModel.UsedSpace = ((drive.TotalSize - drive.AvailableFreeSpace) / (1024 * 1024 * 1024)) + " ГБ";
                                    
                                    // Вычисляем заполненность в процентах
                                    double totalGB = (double)drive.TotalSize / (1024 * 1024 * 1024);
                                    double usedGB = (double)(drive.TotalSize - drive.AvailableFreeSpace) / (1024 * 1024 * 1024);
                                    diskModel.UsagePercent = Math.Round((usedGB / totalGB) * 100, 0);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Ошибка при расчете размера диска {drive.Name}: {ex.Message}", 
                                        "Отладка", MessageBoxButton.OK, MessageBoxImage.Information);
                                    
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
                            _disks.Add(diskModel);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при обработке диска {drive.Name}: {ex.Message}", 
                                "Отладка", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Общая ошибка при загрузке дисков: {ex.Message}", 
                        "Отладка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                // Обновляем источник данных
                try 
                {
                    DisksList.ItemsSource = null;
                    DisksList.ItemsSource = _disks;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении списка: {ex.Message}", 
                        "Отладка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                // Скрываем индикатор загрузки
                _isLoading = false;
                
                try 
                { 
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    _loadingStoryboard?.Stop(); 
                } 
                catch {}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// Закрытие окна
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку быстрой очистки
        /// </summary>
        private void QuickCleanupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем букву диска из тега кнопки
                if (sender is Button button && button.Tag is string driveLetter)
                {
                    // Запрос подтверждения
                    MessageBoxResult result = MessageBox.Show(
                        $"Выполнить быструю очистку диска {driveLetter}?\n\nБудут очищены:\n- Временные файлы\n- Файл гибернации\n- Другие некритические данные",
                        "Подтверждение", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                        
                    if (result != MessageBoxResult.Yes)
                        return;
                        
                    // Преобразуем букву диска к формату C:
                    string driveLetterOnly = driveLetter.Replace("\\", "").TrimEnd(':') + ":";
                    
                    try
                    {
                        // Показываем индикатор выполнения
                        button.IsEnabled = false;
                        
                        // Запускаем процесс очистки временных файлов
                        Task.Run(() => 
                        {
                            try
                            {
                                // Очистка %TEMP% директории
                                CleanTempDirectory(driveLetterOnly);
                                
                                // Очистка файла гибернации, если диск системный
                                if (driveLetterOnly.ToUpper() == Path.GetPathRoot(Environment.SystemDirectory))
                                {
                                    CleanHibernationFile();
                                }
                                
                                // Очистка других некритичных данных
                                CleanOtherTempData(driveLetterOnly);
                                
                                // Обновляем информацию о дисках после очистки
                                Application.Current.Dispatcher.Invoke(() => 
                                {
                                    LoadDisksInfo();
                                    MessageBox.Show(
                                        $"Быстрая очистка диска {driveLetter} выполнена успешно.",
                                        "Очистка завершена", 
                                        MessageBoxButton.OK, 
                                        MessageBoxImage.Information);
                                    button.IsEnabled = true;
                                });
                            }
                            catch (Exception ex)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show(
                                        $"Ошибка при быстрой очистке диска {driveLetter}: {ex.Message}",
                                        "Ошибка", 
                                        MessageBoxButton.OK, 
                                        MessageBoxImage.Error);
                                    button.IsEnabled = true;
                                });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при запуске очистки: {ex.Message}",
                            "Ошибка", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                        button.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка в обработчике нажатия кнопки быстрой очистки: {ex.Message}",
                    "Ошибка", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
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
        /// Очищает временные файлы в директории %TEMP%
        /// </summary>
        private void CleanTempDirectory(string driveLetter)
        {
            try
            {
                // Получаем пути к временным директориям
                string tempPath = Path.GetTempPath();
                string localAppDataTemp = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "Temp");
                
                // Проверяем, что временная директория находится на выбранном диске
                if (tempPath.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    CleanDirectory(tempPath);
                }
                
                if (localAppDataTemp.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    CleanDirectory(localAppDataTemp);
                }
                
                // Проверяем системную временную директорию Windows\Temp
                string winTempPath = Path.Combine(
                    Path.GetPathRoot(Environment.SystemDirectory), 
                    "Windows", 
                    "Temp");
                    
                if (winTempPath.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    CleanDirectory(winTempPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при очистке временной директории: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Очищает файл гибернации (только для системного диска)
        /// </summary>
        private void CleanHibernationFile()
        {
            try
            {
                // Проверяем, выключена ли гибернация
                ProcessStartInfo powercfgInfo = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/h /type full",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                // Проверяем текущее состояние
                Process powercfgProcess = Process.Start(powercfgInfo);
                string output = powercfgProcess.StandardOutput.ReadToEnd();
                powercfgProcess.WaitForExit();
                
                // Если гибернация не выключена, выключаем и удаляем файл
                if (!output.Contains("Гибернация выключена") && !output.Contains("Hibernation is disabled"))
                {
                    // Отключаем гибернацию (требуются права администратора)
                    ProcessStartInfo disableInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/h off",
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    
                    Process disableProcess = Process.Start(disableInfo);
                    disableProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при очистке файла гибернации: {ex.Message}");
                // Не выбрасываем исключение, так как этот шаг не критичен
            }
        }
        
        /// <summary>
        /// Очищает другие временные данные на диске
        /// </summary>
        private void CleanOtherTempData(string driveLetter)
        {
            try
            {
                // Очистка корзины на выбранном диске
                CleanRecycleBin(driveLetter);
                
                // Очистка кэша браузеров
                CleanBrowserCache(driveLetter);
                
                // Очистка корзины Windows Update
                if (driveLetter.Equals(Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    CleanWindowsUpdateCache();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при очистке других временных данных: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Очищает директорию, удаляя все файлы и поддиректории
        /// </summary>
        private void CleanDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;
                    
                DirectoryInfo di = new DirectoryInfo(path);
                
                // Удаление файлов
                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Пропускаем файлы, которые не удалось удалить
                    }
                }
                
                // Удаление директорий
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    try
                    {
                        dir.Delete(true);
                    }
                    catch
                    {
                        // Пропускаем директории, которые не удалось удалить
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при очистке директории {path}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Очищает корзину для указанного диска
        /// </summary>
        private void CleanRecycleBin(string driveLetter)
        {
            try
            {
                // Используем Shell32.dll для очистки корзины
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType != null)
                {
                    object shell = Activator.CreateInstance(shellType);
                    Type recycleBinType = Type.GetTypeFromCLSID(new Guid("645FF040-5081-101B-9F08-00AA002F954E"));
                    object recycleBin = shell.GetType().InvokeMember("NameSpace", 
                        System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { 10 });
                    
                    if (recycleBin != null)
                    {
                        recycleBin.GetType().InvokeMember("Items", 
                            System.Reflection.BindingFlags.InvokeMethod, null, recycleBin, new object[0]);
                        
                        // Это очистит корзину для всех дисков
                        // Ограничение по отдельному диску не предоставляется через этот API
                        MessageBoxResult result = MessageBox.Show(
                            "Очистить корзину для всех дисков?",
                            "Подтверждение", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Question);
                            
                        if (result == MessageBoxResult.Yes)
                        {
                            // Очистка корзины через SHEmptyRecycleBin
                            try
                            {
                                int resultValue = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, 
                                    NativeMethods.RecycleBinFlags.SHERB_NOCONFIRMATION | 
                                    NativeMethods.RecycleBinFlags.SHERB_NOPROGRESSUI);
                                
                                if (resultValue != 0)
                                {
                                    Debug.WriteLine($"Ошибка при очистке корзины: {resultValue}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Ошибка при вызове SHEmptyRecycleBin: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при очистке корзины: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Очищает кэш браузеров на указанном диске
        /// </summary>
        private void CleanBrowserCache(string driveLetter)
        {
            try
            {
                // Chrome
                string chromeCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "User Data", "Default", "Cache");
                    
                if (chromeCachePath.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase) && 
                    Directory.Exists(chromeCachePath))
                {
                    CleanDirectory(chromeCachePath);
                }
                
                // Edge
                string edgeCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Edge", "User Data", "Default", "Cache");
                    
                if (edgeCachePath.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase) && 
                    Directory.Exists(edgeCachePath))
                {
                    CleanDirectory(edgeCachePath);
                }
                
                // Firefox
                string firefoxProfilesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Mozilla", "Firefox", "Profiles");
                    
                if (firefoxProfilesPath.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase) && 
                    Directory.Exists(firefoxProfilesPath))
                {
                    foreach (string profileDir in Directory.GetDirectories(firefoxProfilesPath))
                    {
                        string cachePath = Path.Combine(profileDir, "cache2");
                        if (Directory.Exists(cachePath))
                        {
                            CleanDirectory(cachePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при очистке кэша браузеров: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Очищает кэш обновлений Windows
        /// </summary>
        private void CleanWindowsUpdateCache()
        {
            try
            {
                // Этот метод требует прав администратора
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "stop wuauserv",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };
                
                Process process = Process.Start(startInfo);
                process.WaitForExit();
                
                string windowsUpdatePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "SoftwareDistribution");
                    
                if (Directory.Exists(windowsUpdatePath))
                {
                    // Попытка переименовать и удалить директорию
                    try
                    {
                        string backup = windowsUpdatePath + ".old";
                        if (Directory.Exists(backup))
                        {
                            Directory.Delete(backup, true);
                        }
                        
                        Directory.Move(windowsUpdatePath, backup);
                        Directory.CreateDirectory(windowsUpdatePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Не удалось переименовать/удалить SoftwareDistribution: {ex.Message}");
                    }
                }
                
                // Запускаем службу обновлений Windows
                startInfo = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "start wuauserv",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };
                
                process = Process.Start(startInfo);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при очистке кэша Windows Update: {ex.Message}");
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