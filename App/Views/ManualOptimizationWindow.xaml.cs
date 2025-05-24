using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace App.Views
{
    /// <summary>
    /// Логика взаимодействия для ManualOptimizationWindow.xaml
    /// </summary>
    public partial class ManualOptimizationWindow : Window
    {
        // Инициализируем таймер для анимации
        private readonly DispatcherTimer _dotsAnimationTimer = new DispatcherTimer();
        private int _dotAnimationStep = 0;
        private Storyboard? _loadingRotationStoryboard;
        
        // Список чекбоксов для оптимизаций
        private List<CheckBox> _optimizationCheckBoxes = new List<CheckBox>();
        
        // Флаг для определения результата диалогового окна
        private bool _optimizationApplied = false;
        
        // Константы для взаимодействия с API Windows
        private const int SRRC_APP_INSTALL = 0;
        private const int BEGIN_SYSTEM_CHANGE = 100;
        private const int END_SYSTEM_CHANGE = 101;
        private const int BEGIN_NESTED_SYSTEM_CHANGE = 102;
        private const int END_NESTED_SYSTEM_CHANGE = 103;

        // Импорт функций SrSetRestorePoint из Windows API для создания точек восстановления
        [DllImport("SrClient.dll", CharSet = CharSet.Unicode)]
        private static extern bool SRSetRestorePoint(ref RESTOREPTINFO pRestorePtSpec, ref STATEMGRSTATUS pSMgrStatus);

        // Структуры для создания точки восстановления
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RESTOREPTINFO
        {
            public int dwEventType;
            public int dwRestorePtType;
            public long llSequenceNumber;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STATEMGRSTATUS
        {
            public int nStatus;
            public long llSequenceNumber;
        }
        
        public ManualOptimizationWindow()
        {
            try
            {
                InitializeComponent();
                
                // Обработчик загрузки окна
                Loaded += ManualOptimizationWindow_Loaded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании окна мануальной оптимизации: {ex.Message}");
                MessageBox.Show($"Не удалось открыть окно мануальной оптимизации: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ManualOptimizationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Центрируем окно относительно владельца
                if (Owner != null)
                {
                    this.Left = Owner.Left + (Owner.Width - this.Width) / 2;
                    this.Top = Owner.Top + (Owner.Height - this.Height) / 2;
                }
                
                // Настройка анимаций загрузки
                SetupLoadingAnimations();
                
                // Инициализируем список чекбоксов
                InitializeCheckBoxes();
                
                // Обновляем счетчик выбранных твиков
                UpdateSelectedCount(null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке окна мануальной оптимизации: {ex.Message}");
                MessageBox.Show($"Произошла ошибка при загрузке окна: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Инициализация списка чекбоксов
        /// </summary>
        private void InitializeCheckBoxes()
        {
            // Очищаем существующий список
            _optimizationCheckBoxes.Clear();
            
            // Пока что список пуст, так как пользователь сам добавит оптимизации позже
            // Когда будут добавлены новые чекбоксы, их нужно будет добавить в этот список
            
            // Проверяем наличие выбранных оптимизаций и обновляем статус кнопки
            UpdateApplyButtonState();
        }
        
        /// <summary>
        /// Обновляет состояние кнопки применения в зависимости от наличия выбранных оптимизаций
        /// </summary>
        private void UpdateApplyButtonState()
        {
            int selectedCount = 0;
            foreach (var checkBox in _optimizationCheckBoxes)
            {
                if (checkBox.IsChecked == true)
                {
                    selectedCount++;
                }
            }
            
            // Активируем кнопку, только если выбрана хотя бы одна оптимизация
            ApplyOptimizationsButton.IsEnabled = selectedCount > 0;
        }
        
        /// <summary>
        /// Обновляет счетчик выбранных твиков
        /// </summary>
        private void UpdateSelectedCount(object sender, RoutedEventArgs e)
        {
            int selectedCount = 0;
            foreach (var checkBox in _optimizationCheckBoxes)
            {
                if (checkBox.IsChecked == true)
                {
                    selectedCount++;
                }
            }
            
            // Обновляем текст счетчика
            SelectedTweaksCountText.Text = $"Выбрано: {selectedCount} оптимизаций";
            
            // Обновляем состояние кнопки применения
            UpdateApplyButtonState();
        }
        
        /// <summary>
        /// Настраивает анимации индикатора загрузки
        /// </summary>
        private void SetupLoadingAnimations()
        {
            try
            {
                // Настройка анимации вращения
                if (LoadingRotation != null)
                {
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
                }
                
                // Настройка анимации точек
                _dotsAnimationTimer.Interval = TimeSpan.FromMilliseconds(350);
                _dotsAnimationTimer.Tick += (s, e) => 
                {
                    if (Dot1 == null || Dot2 == null || Dot3 == null) return;
                    
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
            catch (Exception ex)
            {
                // Логирование ошибки или вывод сообщения
                System.Diagnostics.Debug.WriteLine($"Ошибка при настройке анимаций: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Показывает экран загрузки с указанным сообщением
        /// </summary>
        private void ShowLoadingScreen(string message = "Применение оптимизаций...")
        {
            try
            {
                // Устанавливаем текст загрузки
                if (LoadingText != null)
                    LoadingText.Text = message;
                
                // Показываем панель загрузки
                if (LoadingPanel != null)
                    LoadingPanel.Visibility = Visibility.Visible;
                
                // Запускаем анимацию вращения
                if (_loadingRotationStoryboard != null)
                    _loadingRotationStoryboard.Begin();
                
                // Запускаем анимацию точек
                _dotsAnimationTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при отображении экрана загрузки: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Скрывает экран загрузки
        /// </summary>
        private void HideLoadingScreen()
        {
            try
            {
                // Скрываем панель загрузки
                if (LoadingPanel != null)
                    LoadingPanel.Visibility = Visibility.Collapsed;
                
                // Останавливаем анимацию вращения
                _loadingRotationStoryboard?.Stop();
                
                // Останавливаем анимацию точек
                _dotsAnimationTimer.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при скрытии экрана загрузки: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку "Выбрать все"
        /// </summary>
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in _optimizationCheckBoxes)
            {
                checkBox.IsChecked = true;
            }
            
            // Обновляем счетчик выбранных твиков
            UpdateSelectedCount(null, null);
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку "Снять все"
        /// </summary>
        private void UnselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in _optimizationCheckBoxes)
            {
                checkBox.IsChecked = false;
            }
            
            // Обновляем счетчик выбранных твиков
            UpdateSelectedCount(null, null);
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку "Применить оптимизации"
        /// </summary>
        private async void ApplyOptimizationsButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем экран загрузки
            ShowLoadingScreen("Применение выбранных оптимизаций...");
            
            try
            {
                // Создаем точку восстановления, если необходимо
                if (CreateRestorePointCheckBox.IsChecked == true)
                {
                    ShowLoadingScreen("Создание точки восстановления системы...");
                    bool restorePointCreated = await CreateSystemRestorePoint("Оптимизация системы - ручные настройки");
                    
                    if (restorePointCreated)
                    {
                        Debug.WriteLine("Точка восстановления создана успешно");
                    }
                    else
                    {
                        MessageBox.Show("Не удалось создать точку восстановления. Продолжить применение оптимизаций?",
                            "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    }
                    
                    ShowLoadingScreen("Применение выбранных оптимизаций...");
                }
                
                // Применяем выбранные оптимизации
                await ApplySelectedOptimizations();
                
                // Устанавливаем флаг успешного применения
                _optimizationApplied = true;
                
                // Закрываем окно с результатом true
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при применении оптимизаций: {ex.Message}", 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Скрываем экран загрузки
                HideLoadingScreen();
            }
        }
        
        /// <summary>
        /// Применяет выбранные оптимизации
        /// </summary>
        private async Task ApplySelectedOptimizations()
        {
            // Здесь будет код применения выбранных оптимизаций
            // Пока он пуст, так как пользователь еще не добавил конкретные оптимизации
            
            // Имитация процесса
            await Task.Delay(2000);
            
            // Скрываем экран загрузки
            HideLoadingScreen();
        }
        
        /// <summary>
        /// Создает точку восстановления системы Windows
        /// </summary>
        /// <param name="description">Описание точки восстановления</param>
        /// <returns>True если точка восстановления создана успешно</returns>
        private async Task<bool> CreateSystemRestorePoint(string description)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Проверяем, включена ли служба Восстановления системы
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "powershell.exe";
                        process.StartInfo.Arguments = "-Command \"(Get-ComputerRestorePoint -ErrorAction SilentlyContinue) -ne $null\"";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        
                        // Если службы отключены или не ответили корректно
                        if (!output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase))
                        {
                            // Пробуем включить службы
                            EnableSystemRestore();
                        }
                    }
                    
                    // Создаем точку восстановления
                    RESTOREPTINFO restorePtInfo = new RESTOREPTINFO();
                    STATEMGRSTATUS status = new STATEMGRSTATUS();
                    
                    restorePtInfo.dwEventType = BEGIN_SYSTEM_CHANGE;
                    restorePtInfo.dwRestorePtType = SRRC_APP_INSTALL;
                    restorePtInfo.szDescription = description;
                    
                    bool success = SRSetRestorePoint(ref restorePtInfo, ref status);
                    
                    if (!success || status.nStatus != 0)
                    {
                        Debug.WriteLine($"Не удалось создать точку восстановления. Код ошибки: {status.nStatus}");
                        
                        // Пробуем создать точку восстановления через PowerShell
                        return CreateRestorePointWithPowerShell(description);
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при создании точки восстановления: {ex.Message}");
                    // Пробуем резервный метод через PowerShell
                    return CreateRestorePointWithPowerShell(description);
                }
            });
        }
        
        /// <summary>
        /// Включает службу восстановления системы, если она отключена
        /// </summary>
        private void EnableSystemRestore()
        {
            try
            {
                // Запускаем PowerShell с правами администратора для включения System Restore
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = "-Command \"Enable-ComputerRestore -Drive 'C:\\'\"";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas"; // Запуск с правами администратора
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit(10000); // Ждем 10 секунд максимум
                }
                
                // Проверяем, что система включена
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "vssadmin.exe";
                    process.StartInfo.Arguments = "list shadowstorage";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при включении службы восстановления: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Резервный метод создания точки восстановления через PowerShell
        /// </summary>
        private bool CreateRestorePointWithPowerShell(string description)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = $"-Command \"Checkpoint-Computer -Description '{description}' -RestorePointType 'APPLICATION_INSTALL'\"";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas"; // Запуск с правами администратора
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    
                    // Ожидаем завершения процесса (может занять время)
                    process.WaitForExit(30000); // Ждем 30 секунд максимум
                    
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при создании точки восстановления через PowerShell: {ex.Message}");
                return false;
            }
        }
        
        // Для перетаскивания окна
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        
        // Обработчик нажатия кнопки "Закрыть"
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Останавливаем таймеры перед закрытием
                if (_dotsAnimationTimer != null)
                    _dotsAnimationTimer.Stop();
                
                if (_loadingRotationStoryboard != null)
                    _loadingRotationStoryboard.Stop();
                
                this.DialogResult = _optimizationApplied;
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии окна: {ex.Message}");
                // Принудительно закрываем окно в случае ошибки
                try { this.Close(); } catch { }
            }
        }
    }
} 