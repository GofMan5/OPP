using System;
using System.Windows;
using System.Windows.Input;

namespace App.Views
{
    /// <summary>
    /// Логика взаимодействия для AdminRequiredDialog.xaml
    /// </summary>
    public partial class AdminRequiredDialog : Window
    {
        /// <summary>
        /// Результат диалога: true - продолжить без прав администратора, false - закрыть приложение
        /// </summary>
        public bool ContinueWithoutAdmin { get; private set; }

        public AdminRequiredDialog()
        {
            InitializeComponent();
            ContinueWithoutAdmin = false;
        }

        // Обработчик для перетаскивания окна
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            ContinueWithoutAdmin = true;
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ContinueWithoutAdmin = false;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Статический метод для показа диалога и получения результата
        /// </summary>
        /// <returns>true, если пользователь решил продолжить без прав администратора, иначе false</returns>
        public static bool ShowAdminRequiredDialog()
        {
            AdminRequiredDialog dialog = new AdminRequiredDialog();
            bool? result = dialog.ShowDialog();
            
            return result == true && dialog.ContinueWithoutAdmin;
        }
    }
} 