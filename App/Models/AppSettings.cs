namespace App.Models
{
    /// <summary>
    /// Модель для настроек приложения
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Имя приложения
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Версия приложения
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Требуются ли права администратора для работы приложения
        /// </summary>
        public bool RequiresAdmin { get; set; } = true;
    }
} 