using System;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.Reflection;
using System.Diagnostics;

namespace App.Helpers
{
    /// <summary>
    /// Класс для управления автозапуском программы с Windows
    /// </summary>
    public static class AutoStartManager
    {
        private const string REG_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private static readonly string _appName = AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "");
        
        /// <summary>
        /// Включает или отключает автозапуск приложения с Windows
        /// </summary>
        /// <param name="enable">true - включить автозапуск, false - отключить</param>
        /// <param name="startMinimized">true - запускать в свернутом состоянии</param>
        public static void SetAutoStart(bool enable, bool startMinimized = false)
        {
            try
            {
                // Получаем путь к исполняемому файлу приложения
                var process = Process.GetCurrentProcess();
                var mainModule = process.MainModule;
                
                if (mainModule == null)
                {
                    throw new Exception("Не удалось получить информацию о главном модуле приложения");
                }
                
                string appPath = mainModule.FileName;
                
                // Формируем аргументы командной строки для запуска приложения
                string commandLine = startMinimized ? $"\"{appPath}\" --minimized" : $"\"{appPath}\"";
                
                // Открываем или создаем ключ реестра для автозапуска
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(REG_KEY_PATH, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            // Добавляем приложение в автозапуск
                            key.SetValue(_appName, commandLine);
                        }
                        else
                        {
                            // Удаляем приложение из автозапуска
                            if (key.GetValue(_appName) != null)
                            {
                                key.DeleteValue(_appName, false);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Не удалось открыть ключ реестра для автозапуска");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке автозапуска: {ex.Message}", 
                    "Ошибка настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Проверяет, установлен ли автозапуск для приложения
        /// </summary>
        /// <returns>true, если автозапуск включен</returns>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(REG_KEY_PATH, false))
                {
                    if (key != null)
                    {
                        return key.GetValue(_appName) != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при проверке автозапуска: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Проверяет, запущено ли приложение с параметром --minimized
        /// </summary>
        /// <returns>true, если приложение запущено с параметром --minimized</returns>
        public static bool ShouldStartMinimized()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                return Array.Exists(args, arg => arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }
    }
} 