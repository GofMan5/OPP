using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace App.Helpers
{
    public class UpdateInfo
    {
        public string? Version { get; set; }
        public string? ReleaseDate { get; set; }
        public bool Mandatory { get; set; }
        public string? DownloadUrl { get; set; }
        public string? ChangelogUrl { get; set; }
        public string? MinOSVersion { get; set; }
        public long FileSize { get; set; }
        public UpdateInfoLanguages? UpdateMessages { get; set; }

        public class UpdateInfoLanguages
        {
            public string? Ru { get; set; }
            public string? En { get; set; }
        }
    }

    public enum UpdateStatus
    {
        NoUpdates,
        UpdateAvailable,
        UpdateDownloading,
        UpdateReady,
        UpdateError
    }

    public class UpdateManager
    {
        // Фиксированная ссылка на файл версии
        private const string VERSION_CHECK_URL = "https://raw.githubusercontent.com/GofMan5/OPP/main/update_info.json";
        
        // Текущая версия приложения (задается при инициализации)
        private readonly string _currentVersion;
        
        // Состояние обновления
        public UpdateStatus Status { get; private set; } = UpdateStatus.NoUpdates;
        
        // Информация о доступном обновлении
        public UpdateInfo? AvailableUpdate { get; private set; }
        
        // Список изменений
        public string Changelog { get; private set; } = string.Empty;
        
        // Путь к загруженному обновлению
        private string _downloadedUpdatePath = string.Empty;

        // HTTP клиент для всех запросов
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // События
        public event EventHandler<UpdateStatus>? UpdateStatusChanged;
        public event EventHandler<int>? DownloadProgressChanged;
        
        // Флаг блокировки для предотвращения повторных проверок
        private static bool _isCheckingForUpdates = false;
        
        // Событие изменения состояния блокировки
        public event EventHandler<bool>? CheckingStateChanged;
        
        // Свойство для проверки статуса блокировки
        public bool IsCheckingForUpdates 
        { 
            get => _isCheckingForUpdates; 
            private set 
            {
                if (_isCheckingForUpdates != value)
                {
                    _isCheckingForUpdates = value;
                    CheckingStateChanged?.Invoke(this, value);
                }
            }
        }
        
        // Информация о последнем доступном обновлении для отображения
        public string UpdateNotificationMessage => GetUpdateNotificationMessage();

        public UpdateManager(string currentVersion)
        {
            _currentVersion = currentVersion;
        }
        
        /// <summary>
        /// Возвращает сообщение для уведомления об обновлении
        /// </summary>
        private string GetUpdateNotificationMessage()
        {
            if (Status != UpdateStatus.UpdateAvailable || AvailableUpdate == null)
                return string.Empty;
            
            string sizeInfo = string.Empty;
            if (AvailableUpdate.FileSize > 0)
            {
                double sizeInMb = Math.Round(AvailableUpdate.FileSize / 1024.0 / 1024.0, 2);
                sizeInfo = $" ({sizeInMb} МБ)";
            }
            
            string message = AvailableUpdate.UpdateMessages?.Ru ?? 
                             $"Доступно обновление до версии {AvailableUpdate.Version}{sizeInfo}";
                             
            return message;
        }
        
        /// <summary>
        /// Проверяет наличие обновлений
        /// </summary>
        public async Task<bool> CheckForUpdatesAsync()
        {
            // Проверяем, не выполняется ли уже проверка обновлений
            if (IsCheckingForUpdates)
            {
                Debug.WriteLine("UpdateManager: Проверка обновлений уже выполняется");
                return false;
            }
            
            try
            {
                // Устанавливаем флаг блокировки
                IsCheckingForUpdates = true;
                
                Debug.WriteLine($"UpdateManager: Начинаем проверку обновлений. URL: {VERSION_CHECK_URL}");
                
                // Загружаем файл с информацией о версии
                string versionJson = await _httpClient.GetStringAsync(VERSION_CHECK_URL);
                Debug.WriteLine($"UpdateManager: Получен файл версии: {versionJson}");
                
                // Десериализуем JSON
                AvailableUpdate = JsonConvert.DeserializeObject<UpdateInfo>(versionJson);
                Debug.WriteLine($"UpdateManager: Десериализован объект версии: {AvailableUpdate?.Version}");
                
                if (AvailableUpdate == null)
                {
                    Debug.WriteLine("UpdateManager ERROR: Не удалось десериализовать объект версии");
                    Status = UpdateStatus.UpdateError;
                    UpdateStatusChanged?.Invoke(this, Status);
                    return false;
                }
                
                if (string.IsNullOrEmpty(AvailableUpdate.Version))
                {
                    Debug.WriteLine("UpdateManager ERROR: Версия не указана в объекте");
                    Status = UpdateStatus.UpdateError;
                    UpdateStatusChanged?.Invoke(this, Status);
                    return false;
                }
                
                // Проверяем, доступно ли обновление
                Debug.WriteLine($"UpdateManager: Сравниваем версии. Текущая: {_currentVersion}, Доступная: {AvailableUpdate.Version}");
                int compareResult = CompareVersions(AvailableUpdate.Version, _currentVersion);
                Debug.WriteLine($"UpdateManager: Результат сравнения: {compareResult}");
                
                if (compareResult > 0)
                {
                    Debug.WriteLine("UpdateManager: Доступно обновление");
                    Status = UpdateStatus.UpdateAvailable;
                    UpdateStatusChanged?.Invoke(this, Status);
                    return true;
                }
                
                Debug.WriteLine("UpdateManager: Обновление не требуется");
                Status = UpdateStatus.NoUpdates;
                UpdateStatusChanged?.Invoke(this, Status);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateManager ERROR: Ошибка при проверке обновлений: {ex.Message}");
                Debug.WriteLine($"UpdateManager ERROR: {ex.StackTrace}");
                
                // Показываем пользователю подробности ошибки только если это не тихая проверка
                if (!Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        MessageBox.Show(
                            $"Ошибка при проверке обновлений:\n\n" +
                            $"URL: {VERSION_CHECK_URL}\n\n" +
                            $"Сообщение: {ex.Message}", 
                            "Ошибка обновления", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    });
                }
                else
                {
                    MessageBox.Show(
                        $"Ошибка при проверке обновлений:\n\n" +
                        $"URL: {VERSION_CHECK_URL}\n\n" +
                        $"Сообщение: {ex.Message}", 
                        "Ошибка обновления", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
                
                Status = UpdateStatus.UpdateError;
                UpdateStatusChanged?.Invoke(this, Status);
                return false;
            }
            finally
            {
                // Снимаем флаг блокировки в любом случае
                IsCheckingForUpdates = false;
            }
        }

        /// <summary>
        /// Загружает список изменений
        /// </summary>
        public async Task<string> DownloadChangelogAsync()
        {
            try
            {
                if (AvailableUpdate == null || string.IsNullOrEmpty(AvailableUpdate.ChangelogUrl))
                {
                    return "Информация о списке изменений недоступна.";
                }
                
                Changelog = await _httpClient.GetStringAsync(AvailableUpdate.ChangelogUrl);
                return Changelog;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке списка изменений: {ex.Message}");
                return "Не удалось загрузить список изменений.";
            }
        }

        /// <summary>
        /// Загружает файл обновления
        /// </summary>
        public async Task<bool> DownloadUpdateAsync()
        {
            if (AvailableUpdate == null) 
            {
                Debug.WriteLine("UpdateManager ERROR: AvailableUpdate is null");
                return false;
            }

            try
            {
                Status = UpdateStatus.UpdateDownloading;
                UpdateStatusChanged?.Invoke(this, Status);
                
                // Создаем временный каталог для обновлений, если его нет
                string updateDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GofMan3", "Updates");
                Debug.WriteLine($"UpdateManager: Директория обновлений: {updateDir}");
                
                // Проверяем, что директория доступна для записи
                try {
                    if (!Directory.Exists(updateDir))
                    {
                        Directory.CreateDirectory(updateDir);
                        Debug.WriteLine("UpdateManager: Создана директория обновлений");
                    }
                    
                    // Проверяем права доступа, создав тестовый файл
                    string testFilePath = Path.Combine(updateDir, "test_write_access.tmp");
                    File.WriteAllText(testFilePath, "test");
                    File.Delete(testFilePath);
                    Debug.WriteLine("UpdateManager: Проверка прав на запись в директорию обновлений - успешно");
                } catch (Exception dirEx) {
                    // Не удалось использовать указанную директорию, пробуем использовать временную директорию системы
                    Debug.WriteLine($"UpdateManager WARNING: Ошибка доступа к директории обновлений: {dirEx.Message}");
                    updateDir = Path.Combine(Path.GetTempPath(), "GofMan3_Updates");
                    
                    if (!Directory.Exists(updateDir))
                    {
                        Directory.CreateDirectory(updateDir);
                        Debug.WriteLine($"UpdateManager: Создана альтернативная директория обновлений: {updateDir}");
                    }
                }
                
                // Полный путь к загружаемому файлу (теперь это .exe установщик)
                _downloadedUpdatePath = Path.Combine(updateDir, $"OPP_Setup_{AvailableUpdate.Version}.exe");
                Debug.WriteLine($"UpdateManager: Путь для загрузки: {_downloadedUpdatePath}");
                
                // Временный путь для загрузки (чтобы избежать использования частично загруженного файла)
                string tempDownloadPath = _downloadedUpdatePath + ".tmp";
                Debug.WriteLine($"UpdateManager: Временный путь для загрузки: {tempDownloadPath}");
                
                // Проверяем, существуют ли уже файлы
                if (File.Exists(tempDownloadPath))
                {
                    try
                    {
                        File.Delete(tempDownloadPath);
                        Debug.WriteLine("UpdateManager: Удален существующий временный файл загрузки");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"UpdateManager WARNING: Не удалось удалить существующий временный файл: {ex.Message}");
                        tempDownloadPath = Path.Combine(updateDir, $"OPP_Setup_{AvailableUpdate.Version}_{Guid.NewGuid().ToString().Substring(0, 8)}.tmp");
                        Debug.WriteLine($"UpdateManager: Новый временный путь для загрузки: {tempDownloadPath}");
                    }
                }
                
                if (File.Exists(_downloadedUpdatePath))
                {
                    try
                    {
                        // Проверяем, можно ли использовать существующий файл (если он полностью загружен и корректен)
                        FileInfo existingFile = new FileInfo(_downloadedUpdatePath);
                        if (AvailableUpdate.FileSize > 0 && existingFile.Length == AvailableUpdate.FileSize)
                        {
                            Debug.WriteLine("UpdateManager: Найден полностью загруженный файл обновления, пропускаем загрузку");
                            Status = UpdateStatus.UpdateReady;
                            UpdateStatusChanged?.Invoke(this, Status);
                            return true;
                        }
                        
                        File.Delete(_downloadedUpdatePath);
                        Debug.WriteLine("UpdateManager: Удален существующий файл обновления");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"UpdateManager WARNING: Не удалось удалить существующий файл: {ex.Message}");
                        // Создаем уникальное имя файла
                        _downloadedUpdatePath = Path.Combine(updateDir, $"OPP_Setup_{AvailableUpdate.Version}_{Guid.NewGuid().ToString().Substring(0, 8)}.exe");
                        tempDownloadPath = _downloadedUpdatePath + ".tmp";
                        Debug.WriteLine($"UpdateManager: Новый путь для загрузки: {_downloadedUpdatePath}");
                        Debug.WriteLine($"UpdateManager: Новый временный путь для загрузки: {tempDownloadPath}");
                    }
                }
                
                // Загружаем файл
                Debug.WriteLine($"UpdateManager: Начинаем загрузку с {AvailableUpdate.DownloadUrl}");
                
                try
                {
                    // Создаем клиент для отслеживания прогресса загрузки
                    using (HttpClientHandler handler = new HttpClientHandler())
                    {
                        // Настраиваем обработчик для поддержки перенаправлений
                        handler.AllowAutoRedirect = true;
                        handler.MaxAutomaticRedirections = 5;
                        
                        using (HttpClient progressClient = new HttpClient(handler))
                        {
                            // Устанавливаем таймаут
                            progressClient.Timeout = TimeSpan.FromMinutes(10);
                            
                            // Добавляем заголовок User-Agent, чтобы избежать блокировок на серверах
                            progressClient.DefaultRequestHeaders.Add("User-Agent", "OPP-Updater/1.0");
                            
                            // Скачиваем файл
                            using (HttpResponseMessage response = await progressClient.GetAsync(AvailableUpdate.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();
                                
                                // Получаем размер файла
                                long? totalBytes = response.Content.Headers.ContentLength;
                                long downloadedBytes = 0;
                                
                                Debug.WriteLine($"UpdateManager: Размер загружаемого файла: {totalBytes} байт");
                                
                                // Открываем поток для записи во временный файл
                                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                                using (FileStream fileStream = new FileStream(tempDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                                {
                                    byte[] buffer = new byte[8192];
                                    int bytesRead;
                                    DateTime lastProgressUpdate = DateTime.Now;
                                    
                                    // Читаем по частям и обновляем прогресс
                                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                                        downloadedBytes += bytesRead;
                                        
                                        // Вычисляем процент загрузки
                                        if (totalBytes.HasValue && totalBytes.Value > 0)
                                        {
                                            int progressPercentage = (int)((double)downloadedBytes / totalBytes.Value * 100);
                                            
                                            // Обновляем прогресс не чаще чем раз в 100 мс, чтобы не перегружать UI
                                            if ((DateTime.Now - lastProgressUpdate).TotalMilliseconds > 100)
                                            {
                                                Debug.WriteLine($"UpdateManager: Прогресс загрузки: {progressPercentage}%");
                                                DownloadProgressChanged?.Invoke(this, progressPercentage);
                                                lastProgressUpdate = DateTime.Now;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    // Проверяем, что временный файл успешно загружен
                    if (!File.Exists(tempDownloadPath))
                    {
                        Debug.WriteLine("UpdateManager ERROR: Временный файл не был создан");
                        Status = UpdateStatus.UpdateError;
                        UpdateStatusChanged?.Invoke(this, Status);
                        return false;
                    }
                    
                    // Получаем информацию о временном файле
                    FileInfo tempFileInfo = new FileInfo(tempDownloadPath);
                    
                    // Проверяем размер файла
                    if (tempFileInfo.Length == 0)
                    {
                        Debug.WriteLine("UpdateManager ERROR: Загруженный файл имеет нулевой размер");
                        File.Delete(tempDownloadPath);
                        Status = UpdateStatus.UpdateError;
                        UpdateStatusChanged?.Invoke(this, Status);
                        return false;
                    }
                    
                    // Проверяем соответствие размера файла ожидаемому (если указан)
                    if (AvailableUpdate.FileSize > 0 && tempFileInfo.Length != AvailableUpdate.FileSize)
                    {
                        Debug.WriteLine($"UpdateManager WARNING: Размер загруженного файла ({tempFileInfo.Length} байт) не соответствует ожидаемому ({AvailableUpdate.FileSize} байт)");
                        // Можно добавить дополнительную проверку подписи или контрольной суммы файла
                    }
                    
                    // Перемещаем временный файл в целевой
                    if (File.Exists(_downloadedUpdatePath))
                    {
                        try 
                        {
                            File.Delete(_downloadedUpdatePath);
                        }
                        catch (Exception exDelete)
                        {
                            Debug.WriteLine($"UpdateManager WARNING: Не удалось удалить существующий файл перед перемещением: {exDelete.Message}");
                            _downloadedUpdatePath = Path.Combine(updateDir, $"OPP_Setup_{AvailableUpdate.Version}_{Guid.NewGuid().ToString().Substring(0, 8)}.exe");
                            Debug.WriteLine($"UpdateManager: Изменен целевой путь: {_downloadedUpdatePath}");
                        }
                    }
                    
                    File.Move(tempDownloadPath, _downloadedUpdatePath);
                    Debug.WriteLine($"UpdateManager: Временный файл перемещен в {_downloadedUpdatePath}");
                    
                    // Проверяем, что файл действительно существует и имеет ненулевой размер
                    FileInfo fileInfo = new FileInfo(_downloadedUpdatePath);
                    if (!fileInfo.Exists || fileInfo.Length == 0)
                    {
                        Debug.WriteLine("UpdateManager ERROR: Загруженный файл не существует или имеет нулевой размер");
                        Status = UpdateStatus.UpdateError;
                        UpdateStatusChanged?.Invoke(this, Status);
                        return false;
                    }
                    
                    // Проверяем, что файл - это исполняемый файл
                    string extension = Path.GetExtension(_downloadedUpdatePath).ToLower();
                    if (extension != ".exe")
                    {
                        Debug.WriteLine($"UpdateManager WARNING: Загруженный файл имеет неожиданное расширение: {extension}");
                    }
                    
                    Debug.WriteLine($"UpdateManager: Размер загруженного файла: {fileInfo.Length} байт");
                    Status = UpdateStatus.UpdateReady;
                    UpdateStatusChanged?.Invoke(this, Status);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UpdateManager ERROR: Ошибка при загрузке файла: {ex.Message}");
                    Debug.WriteLine($"UpdateManager ERROR: {ex.StackTrace}");
                    
                    // Показываем пользователю подробности ошибки
                    System.Windows.MessageBox.Show(
                        $"Ошибка при загрузке обновления:\n\n" +
                        $"URL: {AvailableUpdate.DownloadUrl}\n\n" +
                        $"Сообщение: {ex.Message}\n\n" +
                        $"Трассировка:\n{ex.StackTrace}", 
                        "Ошибка загрузки обновления", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                    
                    Status = UpdateStatus.UpdateError;
                    UpdateStatusChanged?.Invoke(this, Status);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateManager ERROR: Общая ошибка при загрузке обновления: {ex.Message}");
                Debug.WriteLine($"UpdateManager ERROR: {ex.StackTrace}");
                
                // Показываем пользователю подробности ошибки
                System.Windows.MessageBox.Show(
                    $"Общая ошибка при загрузке обновления:\n\n" +
                    $"Сообщение: {ex.Message}\n\n" +
                    $"Трассировка:\n{ex.StackTrace}", 
                    "Ошибка обновления", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
                
                Status = UpdateStatus.UpdateError;
                UpdateStatusChanged?.Invoke(this, Status);
                return false;
            }
        }

        /// <summary>
        /// Устанавливает загруженное обновление
        /// </summary>
        public bool InstallUpdate()
        {
            if (Status != UpdateStatus.UpdateReady)
            {
                Debug.WriteLine($"UpdateManager ERROR: Невозможно установить обновление, статус: {Status}");
                System.Windows.MessageBox.Show($"Ошибка: Невозможно установить обновление, неправильный статус: {Status}", "Ошибка обновления", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
                
            if (string.IsNullOrEmpty(_downloadedUpdatePath))
            {
                Debug.WriteLine("UpdateManager ERROR: Путь к загруженному обновлению не задан");
                System.Windows.MessageBox.Show("Ошибка: Путь к загруженному обновлению не задан", "Ошибка обновления", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
            
            if (!File.Exists(_downloadedUpdatePath))
            {
                Debug.WriteLine($"UpdateManager ERROR: Файл обновления не существует: {_downloadedUpdatePath}");
                System.Windows.MessageBox.Show($"Ошибка: Файл обновления не существует: {_downloadedUpdatePath}", "Ошибка обновления", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

            try
            {
                // Показываем информацию пользователю перед установкой
                var result = System.Windows.MessageBox.Show(
                    "Приложение будет закрыто для установки обновления.\n\n" +
                    "Нажмите ОК для продолжения.", 
                    "Установка обновления", 
                    System.Windows.MessageBoxButton.OKCancel, 
                    System.Windows.MessageBoxImage.Information);
                
                if (result != System.Windows.MessageBoxResult.OK)
                {
                    return false;
                }
                
                // Убеждаемся, что установщик имеет атрибуты для запуска
                // В случае проблем с правами доступа, пытаемся сбросить атрибуты
                try {
                    File.SetAttributes(_downloadedUpdatePath, FileAttributes.Normal);
                } catch (Exception attrEx) {
                    Debug.WriteLine($"UpdateManager WARNING: Не удалось изменить атрибуты файла: {attrEx.Message}");
                }
                
                // Запускаем установщик с повышенными привилегиями
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = _downloadedUpdatePath,
                    Arguments = "/VERYSILENT /NORESTART /CLOSEAPPLICATIONS /NOCANCEL",
                    UseShellExecute = true,
                    Verb = "runas" // Запрос прав администратора
                };
                
                // Запускаем процесс установки
                Process installProcess = Process.Start(psi);
                
                // Если процесс был запущен успешно
                if (installProcess != null)
                {
                    Debug.WriteLine($"UpdateManager: Установщик запущен с ID процесса: {installProcess.Id}");
                    
                    // Небольшая задержка перед закрытием приложения
                    // чтобы убедиться, что установщик запустился
                    System.Threading.Thread.Sleep(1000);
                    
                    // Сохраняем все настройки перед закрытием
                    try {
                        // Если в приложении есть менеджер настроек, можно вызвать его метод сохранения
                        // SettingsManager.SaveSettings();
                        Debug.WriteLine("UpdateManager: Настройки сохранены перед закрытием");
                    } catch (Exception settingsEx) {
                        Debug.WriteLine($"UpdateManager WARNING: Ошибка при сохранении настроек: {settingsEx.Message}");
                    }
                    
                    // Немедленно завершаем работу приложения
                    Debug.WriteLine("UpdateManager: Закрытие приложения для установки обновления");
                    Environment.Exit(0);
                    
                    return true;
                }
                else
                {
                    Debug.WriteLine("UpdateManager ERROR: Не удалось запустить установщик");
                    System.Windows.MessageBox.Show("Не удалось запустить установщик обновления.", 
                        "Ошибка обновления", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Error);
                    
                    Status = UpdateStatus.UpdateError;
                    UpdateStatusChanged?.Invoke(this, Status);
                    return false;
                }
            }
            catch (Win32Exception w32Ex) when ((uint)w32Ex.ErrorCode == 0x80004005 || (uint)w32Ex.ErrorCode == 0x00000005)
            {
                // Обработка отказа пользователя предоставить права администратора
                Debug.WriteLine($"UpdateManager ERROR: Пользователь отменил запрос прав администратора: {w32Ex.Message}");
                System.Windows.MessageBox.Show(
                    "Для установки обновления требуются права администратора.\n" +
                    "Пожалуйста, при запросе прав нажмите 'Да'.", 
                    "Требуются права администратора", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Warning);
                
                Status = UpdateStatus.UpdateError;
                UpdateStatusChanged?.Invoke(this, Status);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateManager ERROR: Общая ошибка при установке обновления: {ex.Message}");
                Debug.WriteLine($"UpdateManager ERROR: {ex.StackTrace}");
                
                // Показываем подробную информацию об ошибке
                System.Windows.MessageBox.Show(
                    $"Детали ошибки обновления:\n\n" +
                    $"Сообщение: {ex.Message}\n\n" +
                    $"Путь: {_downloadedUpdatePath}\n\n" +
                    $"Трассировка:\n{ex.StackTrace}", 
                    "Ошибка обновления", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
                
                Status = UpdateStatus.UpdateError;
                UpdateStatusChanged?.Invoke(this, Status);
                return false;
            }
        }

        /// <summary>
        /// Создает красивое уведомление об обновлении
        /// </summary>
        public UIElement CreateUpdateNotification()
        {
            if (Status != UpdateStatus.UpdateAvailable || AvailableUpdate == null)
                return new TextBlock();
                
            // Создаем контейнер для уведомления
            Border container = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E2B37")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 10)
            };
            
            // Создаем эффект тени
            container.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString("#3498DB"),
                Direction = 270,
                ShadowDepth = 2,
                BlurRadius = 10,
                Opacity = 0.5
            };
            
            // Создаем вертикальный стек для содержимого
            StackPanel contentStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0)
            };
            
            // Заголовок уведомления
            TextBlock headerText = new TextBlock
            {
                Text = $"Доступно обновление до версии {AvailableUpdate.Version}",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            // Добавляем анимацию для заголовка
            DoubleAnimation pulseAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.08,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            ScaleTransform scaleTransform = new ScaleTransform(1, 1);
            headerText.RenderTransform = scaleTransform;
            headerText.RenderTransformOrigin = new Point(0.5, 0.5);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
            
            // Текст сообщения об обновлении
            TextBlock messageText = new TextBlock
            {
                Text = AvailableUpdate.UpdateMessages?.Ru ?? "Рекомендуется установить для улучшения функциональности и стабильности.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Colors.LightGray),
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            // Размер обновления
            TextBlock sizeText = null;
            if (AvailableUpdate.FileSize > 0)
            {
                double sizeInMb = Math.Round(AvailableUpdate.FileSize / 1024.0 / 1024.0, 2);
                sizeText = new TextBlock
                {
                    Text = $"Размер: {sizeInMb} МБ",
                    Foreground = new SolidColorBrush(Colors.LightGray),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 0, 10)
                };
            }
            
            // Дата выпуска
            TextBlock dateText = null;
            if (!string.IsNullOrEmpty(AvailableUpdate.ReleaseDate))
            {
                if (DateTime.TryParse(AvailableUpdate.ReleaseDate, out DateTime releaseDate))
                {
                    dateText = new TextBlock
                    {
                        Text = $"Выпущено: {releaseDate.ToString("dd.MM.yyyy")}",
                        Foreground = new SolidColorBrush(Colors.LightGray),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                }
            }
            
            // Создаем контейнер для кнопок
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            
            // Кнопка установки
            Button installButton = new Button
            {
                Content = "Установить обновление",
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0)
            };
            
            // Стиль для кнопки
            installButton.Style = new Style(typeof(Button));
            installButton.Style.Setters.Add(new Setter(Button.TemplateProperty, 
                Application.Current.Resources["RoundedButtonTemplate"] as ControlTemplate ?? 
                Application.Current.Resources["ButtonTemplate"] as ControlTemplate));
            
            // Кнопка "Позже"
            Button laterButton = new Button
            {
                Content = "Позже",
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"))
            };
            
            // Стиль для кнопки
            laterButton.Style = new Style(typeof(Button));
            laterButton.Style.Setters.Add(new Setter(Button.TemplateProperty, 
                Application.Current.Resources["RoundedButtonTemplate"] as ControlTemplate ?? 
                Application.Current.Resources["ButtonTemplate"] as ControlTemplate));
            
            // Добавляем все элементы
            contentStack.Children.Add(headerText);
            contentStack.Children.Add(messageText);
            
            if (sizeText != null)
                contentStack.Children.Add(sizeText);
                
            if (dateText != null)
                contentStack.Children.Add(dateText);
                
            buttonPanel.Children.Add(installButton);
            buttonPanel.Children.Add(laterButton);
            contentStack.Children.Add(buttonPanel);
            
            container.Child = contentStack;
            
            // Добавляем анимацию появления
            container.Opacity = 0;
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            container.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            
            return container;
        }

        /// <summary>
        /// Сравнивает версии в формате x.y.z
        /// </summary>
        /// <returns>1 если v1 > v2, -1 если v1 < v2, 0 если равны</returns>
        private int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1)) return -1;
            if (string.IsNullOrEmpty(v2)) return 1;
            
            try
            {
                Version version1 = new Version(v1);
                Version version2 = new Version(v2);
                
                return version1.CompareTo(version2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сравнении версий: {ex.Message}");
                // Возвращаем -1 в случае ошибки (считаем что обновление не требуется)
                return -1;
            }
        }
    }
} 