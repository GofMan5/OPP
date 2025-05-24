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

        public UpdateManager(string currentVersion)
        {
            _currentVersion = currentVersion;
        }
        
        /// <summary>
        /// Проверяет наличие обновлений
        /// </summary>
        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
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
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateManager ERROR: Ошибка при проверке обновлений: {ex.Message}");
                Debug.WriteLine($"UpdateManager ERROR: {ex.StackTrace}");
                
                // Показываем пользователю подробности ошибки
                System.Windows.MessageBox.Show(
                    $"Ошибка при проверке обновлений:\n\n" +
                    $"URL: {VERSION_CHECK_URL}\n\n" +
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
                
                if (!Directory.Exists(updateDir))
                {
                    Directory.CreateDirectory(updateDir);
                    Debug.WriteLine("UpdateManager: Создана директория обновлений");
                }
                
                // Полный путь к загружаемому файлу (теперь это .exe установщик)
                _downloadedUpdatePath = Path.Combine(updateDir, $"OPP_Setup_{AvailableUpdate.Version}.exe");
                Debug.WriteLine($"UpdateManager: Путь для загрузки: {_downloadedUpdatePath}");
                
                // Проверяем, существует ли уже файл
                if (File.Exists(_downloadedUpdatePath))
                {
                    try
                    {
                        File.Delete(_downloadedUpdatePath);
                        Debug.WriteLine("UpdateManager: Удален существующий файл обновления");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"UpdateManager WARNING: Не удалось удалить существующий файл: {ex.Message}");
                        // Создаем уникальное имя файла
                        _downloadedUpdatePath = Path.Combine(updateDir, $"OPP_Setup_{AvailableUpdate.Version}_{Guid.NewGuid().ToString().Substring(0, 8)}.exe");
                        Debug.WriteLine($"UpdateManager: Новый путь для загрузки: {_downloadedUpdatePath}");
                    }
                }
                
                // Загружаем файл
                Debug.WriteLine($"UpdateManager: Начинаем загрузку с {AvailableUpdate.DownloadUrl}");
                
                try
                {
                    // Создаем клиент для отслеживания прогресса загрузки
                    using (HttpClientHandler handler = new HttpClientHandler())
                    using (HttpClient progressClient = new HttpClient(handler))
                    {
                        // Скачиваем файл
                        using (HttpResponseMessage response = await progressClient.GetAsync(AvailableUpdate.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();
                            
                            // Получаем размер файла
                            long? totalBytes = response.Content.Headers.ContentLength;
                            long downloadedBytes = 0;
                            
                            // Открываем поток для записи в файл
                            using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                            using (FileStream fileStream = new FileStream(_downloadedUpdatePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                
                                // Читаем по частям и обновляем прогресс
                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    downloadedBytes += bytesRead;
                                    
                                    // Вычисляем процент загрузки
                                    if (totalBytes.HasValue && totalBytes.Value > 0)
                                    {
                                        int progressPercentage = (int)((double)downloadedBytes / totalBytes.Value * 100);
                                        Debug.WriteLine($"UpdateManager: Прогресс загрузки: {progressPercentage}%");
                                        DownloadProgressChanged?.Invoke(this, progressPercentage);
                                    }
                                }
                            }
                        }
                    }
                    
                    Debug.WriteLine($"UpdateManager: Файл успешно загружен: {_downloadedUpdatePath}");
                    
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
                
                // Запускаем установщик напрямую
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = _downloadedUpdatePath,
                    Arguments = "/VERYSILENT /NORESTART /CLOSEAPPLICATIONS",
                    UseShellExecute = true
                };
                
                Process.Start(psi);
                
                // Немедленно завершаем работу приложения
                Environment.Exit(0);
                
                return true;
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