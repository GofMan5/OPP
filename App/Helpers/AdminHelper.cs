using System;
using System.Security.Principal;

namespace App.Helpers
{
    public static class AdminHelper
    {
        /// <summary>
        /// Проверяет, запущено ли приложение с правами администратора
        /// </summary>
        /// <returns>true, если приложение запущено с правами администратора, иначе false</returns>
        public static bool IsRunningAsAdmin()
        {
            try
            {
                // Получаем информацию о текущем пользователе Windows
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                // Проверяем, является ли пользователь администратором
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                // В случае ошибки считаем, что пользователь не администратор
                return false;
            }
        }
    }
} 