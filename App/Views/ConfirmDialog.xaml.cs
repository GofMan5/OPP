using System;
using System.Windows;
using System.Windows.Input;

namespace App.Views
{
    /// <summary>
    /// Логика взаимодействия для ConfirmDialog.xaml
    /// </summary>
    public partial class ConfirmDialog : Window
    {
        public enum ConfirmResult
        {
            Yes,
            No,
            Cancel
        }
        
        public ConfirmResult Result { get; private set; } = ConfirmResult.Cancel;
        
        public ConfirmDialog(Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
        }
        
        /// <summary>
        /// Обработчик для перетаскивания окна
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        
        /// <summary>
        /// Обработчик нажатия кнопки "Да"
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ConfirmResult.Yes;
            DialogResult = true;
            Close();
        }
        
        /// <summary>
        /// Обработчик нажатия кнопки "Нет"
        /// </summary>
        private void DontSaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ConfirmResult.No;
            DialogResult = true;
            Close();
        }
        
        /// <summary>
        /// Обработчик нажатия кнопки "Отмена"
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ConfirmResult.Cancel;
            DialogResult = false;
            Close();
        }
        
        /// <summary>
        /// Показывает диалоговое окно подтверждения с заданным заголовком и сообщением
        /// </summary>
        public static ConfirmResult Show(Window owner, string title = "Подтверждение", string message = "Сохранить изменения перед закрытием?")
        {
            var dialog = new ConfirmDialog(owner);
            
            // Устанавливаем заголовок и сообщение, если они переданы
            if (!string.IsNullOrEmpty(title))
            {
                dialog.Title = title;
            }
            
            if (!string.IsNullOrEmpty(message))
            {
                dialog.MessageText.Text = message;
            }
            
            // Отображаем диалог и возвращаем результат
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
} 