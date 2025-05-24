using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace App.Helpers
{
    /// <summary>
    /// Класс для извлечения встроенных ресурсов приложения
    /// </summary>
    public static class ResourceExtractor
    {
        private static readonly string TempDir = Path.Combine(
            Path.GetTempPath(),
            "OPPApp",
            "Tools");
            
        /// <summary>
        /// Извлекает встроенный ресурс WinDirStat и возвращает путь к исполняемому файлу
        /// </summary>
        public static string ExtractWinDirStat()
        {
            try
            {
                // Создаем директорию для извлечения
                string windirstatDir = Path.Combine(TempDir, "windirstat");
                string exePath = Path.Combine(windirstatDir, "windirstat.exe");
                
                // Проверяем, существует ли уже извлеченный файл
                if (File.Exists(exePath))
                {
                    return exePath;
                }
                
                // Создаем директорию если не существует
                if (!Directory.Exists(windirstatDir))
                {
                    Directory.CreateDirectory(windirstatDir);
                }
                
                // Пробуем найти ресурс с разными вариантами имени (учитывая регистр)
                string[] possibleResourceNames = {
                    "App.Resources.Embedded.windirstat.exe",
                    "App.Resources.Embedded.WinDirStat.exe",
                    "App.Resources.Embedded.WINDIRSTAT.EXE"
                };
                
                bool resourceFound = false;
                
                foreach (string resourceName in possibleResourceNames)
                {
                    try
                    {
                        // Пробуем извлечь ресурс с текущим именем
                        ExtractResourceToFile(resourceName, exePath);
                        resourceFound = true;
                        Debug.WriteLine($"Успешно извлечен ресурс {resourceName}");
                        break; // Выходим из цикла если ресурс найден
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Не удалось извлечь ресурс {resourceName}: {ex.Message}");
                        // Продолжаем проверку следующих имен
                    }
                }
                
                // Проверяем результат извлечения
                if (!resourceFound)
                {
                    Debug.WriteLine("Ни один из вариантов имени ресурса не найден");
                    return null;
                }
                
                return exePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при извлечении WinDirStat: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Извлекает ресурс из сборки в файл
        /// </summary>
        private static void ExtractResourceToFile(string resourceName, string filePath)
        {
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new Exception($"Ресурс {resourceName} не найден в сборке");
                }
                
                using (FileStream fileStream = File.Create(filePath))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }
        }
    }
} 