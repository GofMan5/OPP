using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.Helpers
{
    /// <summary>
    /// Класс для управления настройками приложения
    /// </summary>
    public class SettingsData
    {
        // Настройки внешнего вида
        public bool IsDarkTheme { get; set; } = true;
        public bool EnableAnimations { get; set; } = true;
        public bool EnableRoundedCorners { get; set; } = true;
        
        // Общие настройки
        public bool AutoStart { get; set; } = true;
        public bool StartMinimized { get; set; } = true;
        
        // Настройки обновлений
        public bool CheckUpdatesAtStartup { get; set; } = true;
        public bool AutoInstallUpdates { get; set; } = false;
        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;
    }
    
    public static class SettingsManager
    {
        // Экземпляр настроек
        public static SettingsData Settings { get; private set; } = new SettingsData();
        
        // Имя папки приложения в директории пользователя
        private const string APP_FOLDER_NAME = "GofMan3";
        
        // Имя файла с настройками
        private const string SETTINGS_FILENAME = "settings.json";
        
        // Путь к папке с настройками
        private static readonly string _settingsFolder;
        
        // Путь к файлу настроек
        private static readonly string _settingsFilePath;
        
        // Кэш настроек в памяти
        private static JObject _settings;
        
        static SettingsManager()
        {
            // Определяем путь к папке с настройками в каталоге Documents пользователя
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _settingsFolder = Path.Combine(documentsPath, APP_FOLDER_NAME);
            _settingsFilePath = Path.Combine(_settingsFolder, SETTINGS_FILENAME);
            
            // Создаем директорию, если она не существует
            if (!Directory.Exists(_settingsFolder))
            {
                Directory.CreateDirectory(_settingsFolder);
            }
            
            // Загружаем настройки или создаем новый файл
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _settings = JObject.Parse(json);
                }
                catch
                {
                    // Если файл поврежден, создаем новые настройки
                    _settings = CreateDefaultSettings();
                    SaveSettings();
                }
            }
            else
            {
                // Создаем настройки по умолчанию
                _settings = CreateDefaultSettings();
                SaveSettings();
            }
            
            // Загружаем настройки при первом обращении к классу
            LoadSettings();
        }
        
        /// <summary>
        /// Возвращает путь к папке с настройками приложения
        /// </summary>
        public static string SettingsFolder => _settingsFolder;
        
        /// <summary>
        /// Создает настройки по умолчанию
        /// </summary>
        private static JObject CreateDefaultSettings()
        {
            JObject settings = new JObject
            {
                ["AppSettings"] = new JObject
                {
                    ["Theme"] = "Dark",
                    ["EnableAnimations"] = true,
                    ["EnableRoundedCorners"] = true,
                    ["EnableBlurEffect"] = false,
                    ["EnableTransparency"] = true,
                    ["AutoStart"] = true,
                    ["StartMinimized"] = true
                }
            };
            
            return settings;
        }
        
        /// <summary>
        /// Загружает настройки из файла
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                // Если файл с настройками существует
                if (File.Exists(_settingsFilePath))
                {
                    // Читаем JSON и десериализуем в объект настроек
                    string json = File.ReadAllText(_settingsFilePath);
                    var loadedSettings = JsonConvert.DeserializeObject<SettingsData>(json);
                    if (loadedSettings != null)
                    {
                        Settings = loadedSettings;
                    }
                    else
                    {
                        // Если десериализация вернула null, создаем настройки по умолчанию
                        Settings = new SettingsData();
                        SaveSettings();
                    }
                }
                else
                {
                    // Создаем настройки по умолчанию
                    Settings = new SettingsData();
                    
                    // И сохраняем их
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки создаем настройки по умолчанию
                Settings = new SettingsData();
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Сохраняет настройки в файл
        /// </summary>
        public static void SaveSettings()
        {
            try
            {
                // Создаем директорию, если ее нет
                if (!Directory.Exists(_settingsFolder))
                {
                    Directory.CreateDirectory(_settingsFolder);
                }
                
                // Сериализуем настройки в JSON
                string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                
                // Записываем JSON в файл
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Получает значение настройки по ключу с возможностью указать значение по умолчанию
        /// </summary>
        public static T GetSetting<T>(string key, T defaultValue = default)
        {
            // Тут сохраняем обратную совместимость для старого кода
            if (key == "Theme")
                return (T)(object)(Settings.IsDarkTheme ? "Dark" : "Light");
            else if (key == "EnableAnimations")
                return (T)(object)Settings.EnableAnimations;
            else if (key == "EnableRoundedCorners")
                return (T)(object)Settings.EnableRoundedCorners;
            else if (key == "StartMinimized")
                return (T)(object)Settings.StartMinimized;
            else if (key == "Version")
            {
                // Прямое чтение версии из appsettings.json
                try 
                {
                    string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                    if (File.Exists(appSettingsPath))
                    {
                        string json = File.ReadAllText(appSettingsPath);
                        var jsonObj = JObject.Parse(json);
                        string version = jsonObj["AppSettings"]?["Version"]?.ToString() ?? defaultValue?.ToString() ?? "1.0.0";
                        return (T)(object)version;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при чтении версии из appsettings.json: {ex.Message}");
                }
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Сохраняет значение настройки по ключу
        /// </summary>
        public static void SetSetting<T>(string key, T value)
        {
            // Тут сохраняем обратную совместимость для старого кода
            if (key == "Theme")
                Settings.IsDarkTheme = value?.ToString() == "Dark";
            else if (key == "EnableAnimations" && value != null)
            {
                bool enableAnimations = Convert.ToBoolean(value);
                Settings.EnableAnimations = enableAnimations;
            }
            else if (key == "EnableRoundedCorners" && value != null)
            {
                bool enableRoundedCorners = Convert.ToBoolean(value);
                Settings.EnableRoundedCorners = enableRoundedCorners;
            }
            else if (key == "StartMinimized" && value != null)
            {
                bool startMinimized = Convert.ToBoolean(value);
                Settings.StartMinimized = startMinimized;
            }
            
            SaveSettings();
        }
        
        /// <summary>
        /// Сохраняет несколько настроек за один вызов
        /// </summary>
        /// <param name="settings">Словарь с настройками для сохранения</param>
        public static void SaveAllSettings(Dictionary<string, object> settings)
        {
            try
            {
                if (_settings == null)
                {
                    _settings = CreateDefaultSettings();
                }
                
                if (_settings["AppSettings"] == null)
                {
                    _settings["AppSettings"] = new JObject();
                }
                
                foreach (var setting in settings)
                {
                    _settings["AppSettings"][setting.Key] = JToken.FromObject(setting.Value);
                }
                
                SaveSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", 
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
} 