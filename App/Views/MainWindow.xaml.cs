using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using App.Helpers;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Linq;

namespace App.Views
{
    /// <summary>
    /// Класс для частиц интерактивного эффекта
    /// </summary>
    public class Particle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double SpeedX { get; set; }
        public double SpeedY { get; set; }
        public Ellipse Shape { get; set; }
        public double OriginalSize { get; set; }
        public bool IsActive { get; set; } = true;

        public Particle(double x, double y, double speedX, double speedY, Ellipse shape, double size)
        {
            X = x;
            Y = y;
            SpeedX = speedX;
            SpeedY = speedY;
            Shape = shape;
            OriginalSize = size;
        }

        public void Move(double width, double height)
        {
            X += SpeedX;
            Y += SpeedY;
            
            // Проверяем границы и отражаем частицу
            if (X <= 0 || X >= width)
            {
                SpeedX = -SpeedX;
                X = Math.Max(0, Math.Min(X, width));
            }
            
            if (Y <= 0 || Y >= height)
            {
                SpeedY = -SpeedY;
                Y = Math.Max(0, Math.Min(Y, height));
            }
            
            // Обновляем положение элемента на Canvas только если он активен
            if (IsActive)
            {
                Canvas.SetLeft(Shape, X - Shape.Width / 2);
                Canvas.SetTop(Shape, Y - Shape.Height / 2);
            }
        }
    }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Инициализируем таймер сразу, чтобы избежать предупреждения CS8618
        private readonly DispatcherTimer _dotsAnimationTimer = new DispatcherTimer();
        private int _dotAnimationStep = 0;
        private Storyboard? _loadingRotationStoryboard;
        
        // Поля для интерактивного эффекта
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly List<Line> _lines = new List<Line>();
        private readonly Random _random = new Random();
        private readonly DispatcherTimer _particleTimer = new DispatcherTimer();
        private Point _mousePosition = new Point(0, 0);
        private const int ParticleCount = 30; // Уменьшено количество частиц для оптимизации
        private const double ConnectionDistance = 150; // Оптимальная дистанция соединения
        private const double MouseInfluenceDistance = 200; 
        private const double MouseForce = 1.0; 
        private const int MaxActiveLines = 80; // Уменьшено количество линий для оптимизации
        private bool _isParticleEffectEnabled = true;
        private DateTime _lastFrameTime = DateTime.Now;
        
        // Кэш кистей для линий (избегаем создания новых объектов в каждом кадре)
        private readonly SolidColorBrush[] _lineBrushCache;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Получение названия приложения из настроек
            string? appName = SettingsManager.GetSetting<string>("ApplicationName", "GofMan3");
            if (!string.IsNullOrEmpty(appName))
            {
                Title = appName;
            }
            
            // Получение версии приложения из настроек
            string appVersion = ((App.AppClass)Application.Current).AppVersion;
            VersionText.Text = $"Версия: {appVersion}";
            
            // Инициализация кэша кистей для линий (256 оттенков прозрачности)
            _lineBrushCache = new SolidColorBrush[256];
            for (int i = 0; i < 256; i++)
            {
                _lineBrushCache[i] = new SolidColorBrush(Color.FromArgb((byte)i, 150, 190, 255));
                _lineBrushCache[i].Freeze(); // Замораживаем кисть для оптимизации
            }
            
            // Настройка анимаций загрузки
            SetupLoadingAnimations();
            
            // Инициализация интерактивного эффекта
            InitializeParticles();
            
            // Оптимизация таймера - снижаем частоту обновления эффекта
            _particleTimer.Interval = TimeSpan.FromMilliseconds(25); // ~40 FPS 
            _particleTimer.Tick += UpdateParticles;
            _particleTimer.Start();
            
            // Обработчик события изменения размера окна
            this.SizeChanged += (s, e) => {
                // Переинициализируем частицы при изменении размера окна
                if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) > 50 ||
                    Math.Abs(e.NewSize.Height - e.PreviousSize.Height) > 50)
                {
                    InitializeParticles();
                }
                else
                {
                    // Просто обновляем размер холста
                    ParticleCanvas.Width = e.NewSize.Width;
                    ParticleCanvas.Height = e.NewSize.Height;
                }
            };
        }

        /// <summary>
        /// Инициализирует частицы для интерактивного эффекта
        /// </summary>
        private void InitializeParticles()
        {
            // Очищаем существующие частицы и линии
            foreach (var particle in _particles)
            {
                ParticleCanvas.Children.Remove(particle.Shape);
            }
            
            // Удаляем только видимые линии для оптимизации
            foreach (var line in _lines)
            {
                if (line.Visibility == Visibility.Visible)
                {
                    line.Visibility = Visibility.Hidden;
                }
            }
            
            _particles.Clear();
            
            // Установка размеров Canvas
            ParticleCanvas.Width = ActualWidth;
            ParticleCanvas.Height = ActualHeight;
            
            // Создаем новые частицы
            for (int i = 0; i < ParticleCount; i++)
            {
                // Варьируем размеры для более естественного эффекта
                double size = _random.NextDouble() * 2 + 1.5;
                
                // Создаем форму частицы
                var shape = new Ellipse
                {
                    Width = size,
                    Height = size,
                    // Больше белого цвета для лучшей видимости
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)_random.Next(160, 220),
                        (byte)_random.Next(200, 255),
                        (byte)_random.Next(220, 255),
                        (byte)_random.Next(230, 255))),
                    IsHitTestVisible = false // Отключаем регистрацию событий мыши на частицах
                };
                
                // Добавляем на Canvas
                ParticleCanvas.Children.Add(shape);
                
                // Создаем частицу с случайным положением и скоростью
                double x = _random.NextDouble() * ActualWidth;
                double y = _random.NextDouble() * ActualHeight;
                
                // Более реалистичная скорость с разной величиной для разных частиц
                double speed = _random.NextDouble() * 1.2 + 0.2;
                double angle = _random.NextDouble() * Math.PI * 2;
                double speedX = Math.Cos(angle) * speed;
                double speedY = Math.Sin(angle) * speed;
                
                var particle = new Particle(x, y, speedX, speedY, shape, size);
                _particles.Add(particle);
            }
            
            // Создаем пул линий для соединений между частицами только если нужно
            if (_lines.Count < MaxActiveLines)
            {
                // Определяем, сколько линий нужно добавить
                int linesToAdd = MaxActiveLines - _lines.Count;
                
                for (int i = 0; i < linesToAdd; i++)
                {
                    var line = new Line
                    {
                        Stroke = _lineBrushCache[150], // Используем кэшированную кисть
                        StrokeThickness = 0.7,  // Более тонкие линии для оптимизации
                        IsHitTestVisible = false, // Отключаем регистрацию событий мыши на линиях
                        Visibility = Visibility.Hidden
                    };
                    
                    ParticleCanvas.Children.Add(line);
                    _lines.Add(line);
                }
            }
            
            // Сообщаем сборщику мусора о необходимости очистки
            GC.Collect(0); // Запуск сборки мусора нулевого поколения
        }
        
        /// <summary>
        /// Обновляет положение и соединения частиц
        /// </summary>
        private void UpdateParticles(object? sender, EventArgs e)
        {
            if (_particles.Count == 0 || !_isParticleEffectEnabled) return;
            
            // Ограничение FPS для стабильности и оптимизации
            var currentTime = DateTime.Now;
            var elapsed = (currentTime - _lastFrameTime).TotalMilliseconds;
            if (elapsed < 20) // ~50 FPS максимум
            {
                return;
            }
            _lastFrameTime = currentTime;
            
            // Обновляем размеры Canvas, если окно изменило размер
            ParticleCanvas.Width = ActualWidth;
            ParticleCanvas.Height = ActualHeight;
            
            // Кэшируем значение для оптимизации вычислений
            var mouseX = _mousePosition.X;
            var mouseY = _mousePosition.Y;
            var mouseActive = mouseX > 0 && mouseY > 0 && mouseX < ActualWidth && mouseY < ActualHeight;
            
            // Оптимизация: обрабатываем только часть частиц в каждом кадре для равномерной нагрузки
            int particlesToUpdate = _particles.Count;
            double connectionDistanceSquared = ConnectionDistance * ConnectionDistance;
            
            // Перемещаем частицы
            for (int i = 0; i < particlesToUpdate; i++)
            {
                var particle = _particles[i];
                
                // Проверяем влияние мыши, только если мышь активна
                if (mouseActive)
                {
                    double dx = mouseX - particle.X;
                    double dy = mouseY - particle.Y;
                    double distanceSquared = dx * dx + dy * dy;
                    
                    if (distanceSquared < MouseInfluenceDistance * MouseInfluenceDistance && distanceSquared > 0.1)
                    {
                        double distance = Math.Sqrt(distanceSquared);
                        double force = MouseForce * (1 - distance / MouseInfluenceDistance);
                        
                        // Отталкиваем частицу от курсора
                        particle.SpeedX -= dx / distance * force;
                        particle.SpeedY -= dy / distance * force;
                        
                        // Визуальный эффект: частицы растут при приближении мыши
                        if (distance < MouseInfluenceDistance * 0.3)
                        {
                            double scaleFactor = 1.0 + (0.5 * (1.0 - distance / (MouseInfluenceDistance * 0.3)));
                            particle.Shape.Width = particle.OriginalSize * scaleFactor;
                            particle.Shape.Height = particle.OriginalSize * scaleFactor;
                        }
                        else
                        {
                            // Возвращаем нормальный размер, только если он изменен
                            if (Math.Abs(particle.Shape.Width - particle.OriginalSize) > 0.1)
                            {
                                particle.Shape.Width = particle.OriginalSize;
                                particle.Shape.Height = particle.OriginalSize;
                            }
                        }
                    }
                }
                
                // Ограничиваем максимальную скорость без вычисления квадратного корня, если возможно
                double speedSquared = particle.SpeedX * particle.SpeedX + particle.SpeedY * particle.SpeedY;
                double maxSpeedSquared = 6.0; // 2.5^2 = 6.25, немного упрощаем
                
                if (speedSquared > maxSpeedSquared)
                {
                    double ratio = Math.Sqrt(maxSpeedSquared / speedSquared);
                    particle.SpeedX *= ratio;
                    particle.SpeedY *= ratio;
                }
                
                // Перемещаем частицу
                particle.Move(ActualWidth, ActualHeight);
            }
            
            // Оптимизированный алгоритм соединения частиц
            int lineIndex = 0;
            int skipFactor = 2; // Обрабатываем только часть связей для оптимизации
            
            // Используем простой цикл вместо LINQ для оптимизации
            for (int i = 0; i < _particles.Count && lineIndex < MaxActiveLines; i++)
            {
                var p1 = _particles[i];
                
                // Оптимизация: проверяем соседние частицы только через одну
                for (int j = i + skipFactor; j < _particles.Count && lineIndex < MaxActiveLines; j += skipFactor)
                {
                    var p2 = _particles[j];
                    
                    double dx = p2.X - p1.X;
                    double dy = p2.Y - p1.Y;
                    double distanceSquared = dx * dx + dy * dy;
                    
                    if (distanceSquared < connectionDistanceSquared)
                    {
                        double distance = Math.Sqrt(distanceSquared);
                        
                        // Вычисляем opacity в диапазоне [0..255]
                        byte opacity = (byte)(255 * (1 - distance / ConnectionDistance));
                        
                        // Линия видима только если opacity > 0
                        if (opacity > 0)
                        {
                            var line = _lines[lineIndex];
                            
                            // Устанавливаем координаты линии
                            line.X1 = p1.X;
                            line.Y1 = p1.Y;
                            line.X2 = p2.X;
                            line.Y2 = p2.Y;
                            
                            // Используем кэш кистей вместо создания новых
                            line.Stroke = _lineBrushCache[opacity];
                            line.Visibility = Visibility.Visible;
                            
                            lineIndex++;
                        }
                    }
                }
            }
            
            // Скрываем лишние линии (делаем это только если есть видимые линии, которые нужно скрыть)
            if (lineIndex < _lines.Count && lineIndex > 0)
            {
                for (int i = lineIndex; i < _lines.Count; i++)
                {
                    if (_lines[i].Visibility == Visibility.Visible)
                    {
                        _lines[i].Visibility = Visibility.Hidden;
                    }
                }
            }
        }
        
        /// <summary>
        /// Вычисляет расстояние от точки до линии
        /// </summary>
        private double PointDistanceToLine(double px, double py, double x1, double y1, double x2, double y2)
        {
            // Длина линии
            double lineLength = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            if (lineLength < 0.001) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            
            // Проекция точки на линию
            double t = ((px - x1) * (x2 - x1) + (py - y1) * (y2 - y1)) / (lineLength * lineLength);
            
            // Если проекция находится за пределами отрезка
            if (t < 0.0) return Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            if (t > 1.0) return Math.Sqrt((px - x2) * (px - x2) + (py - y2) * (py - y2));
            
            // Ближайшая точка на линии
            double nearestX = x1 + t * (x2 - x1);
            double nearestY = y1 + t * (y2 - y1);
            
            // Расстояние от точки до ближайшей точки на линии
            return Math.Sqrt((px - nearestX) * (px - nearestX) + (py - nearestY) * (py - nearestY));
        }
        
        /// <summary>
        /// Обрабатывает движение мыши на уровне окна для работы эффекта при наведении на кнопки
        /// </summary>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Получаем координаты относительно ParticleCanvas
            _mousePosition = e.GetPosition(ParticleCanvas);
            
            // Предотвращаем исполнение лишних действий при быстром движении мыши
            // Используем простую оптимизацию: реагируем только на каждое n-ое движение мыши
            if (_random.NextDouble() > 0.3)
            {
                return;
            }
            
            // Дополнительно активизируем частицы вокруг курсора при его движении
            // но делаем это редко для оптимизации
            if (_particles.Count > 0 && _random.NextDouble() < 0.1)
            {
                // Находим ближайшие к курсору частицы
                double minDist1 = double.MaxValue;
                double minDist2 = double.MaxValue;
                Particle? closest1 = null;
                Particle? closest2 = null;
                
                // Ищем 2 ближайшие частицы без полного сортирования списка
                foreach (var p in _particles)
                {
                    double dx = p.X - _mousePosition.X;
                    double dy = p.Y - _mousePosition.Y;
                    double distSquared = dx * dx + dy * dy;
                    
                    if (distSquared < minDist1)
                    {
                        minDist2 = minDist1;
                        closest2 = closest1;
                        minDist1 = distSquared;
                        closest1 = p;
                    }
                    else if (distSquared < minDist2)
                    {
                        minDist2 = distSquared;
                        closest2 = p;
                    }
                }
                
                // Добавляем небольшое случайное ускорение только ближайшим частицам
                if (closest1 != null)
                {
                    closest1.SpeedX += (_random.NextDouble() - 0.5) * 0.6;
                    closest1.SpeedY += (_random.NextDouble() - 0.5) * 0.6;
                }
                
                if (closest2 != null)
                {
                    closest2.SpeedX += (_random.NextDouble() - 0.5) * 0.4;
                    closest2.SpeedY += (_random.NextDouble() - 0.5) * 0.4;
                }
            }
        }
        
        /// <summary>
        /// Настраивает анимации индикатора загрузки
        /// </summary>
        private void SetupLoadingAnimations()
        {
            // Настройка анимации вращения
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
            
            // Настройка анимации точек
            _dotsAnimationTimer.Interval = TimeSpan.FromMilliseconds(350);
            _dotsAnimationTimer.Tick += (s, e) => 
            {
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
        
        // Для перетаскивания окна
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        
        // Управление окном
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OptimizationButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем индикатор загрузки и запускаем его анимацию
            ShowLoadingIndicator();
            
            try
            {
                // Создаем и открываем окно оптимизации
                OptimizationWindow optimizationWindow = new OptimizationWindow();
                
                // Скрываем индикатор загрузки перед показом окна
                HideLoadingIndicator();
                
                // Открываем окно оптимизации
                optimizationWindow.Show();
                
                // Закрываем главное окно
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна оптимизации: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Скрываем индикатор загрузки в случае ошибки
                HideLoadingIndicator();
            }
        }

        private void CleanupButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем индикатор загрузки и запускаем его анимацию
            ShowLoadingIndicator();
            
            try
            {
                // Создаем и открываем окно информации о дисках
                DiskInfoWindow diskInfoWindow = new DiskInfoWindow();
                
                // Скрываем индикатор загрузки перед показом окна
                HideLoadingIndicator();
                
                // Открываем окно информации о дисках
                diskInfoWindow.Show();
                
                // Закрываем главное окно
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии информации о дисках: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Скрываем индикатор загрузки в случае ошибки
                HideLoadingIndicator();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем индикатор загрузки и запускаем его анимацию
            ShowLoadingIndicator();
            
            try
            {
                // Создаем и открываем окно настроек
                SettingsWindow settingsWindow = new SettingsWindow();
                
                // Скрываем индикатор загрузки перед показом окна
                HideLoadingIndicator();
                
                // Открываем окно настроек
                settingsWindow.Show();
                
                // Закрываем главное окно
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна настроек: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Скрываем индикатор загрузки в случае ошибки
                HideLoadingIndicator();
            }
        }
        
        /// <summary>
        /// Отображает индикатор загрузки с анимацией
        /// </summary>
        private void ShowLoadingIndicator()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            _loadingRotationStoryboard?.Begin();
            _dotsAnimationTimer.Start();
        }
        
        /// <summary>
        /// Скрывает индикатор загрузки и останавливает анимацию
        /// </summary>
        private void HideLoadingIndicator()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            _loadingRotationStoryboard?.Stop();
            _dotsAnimationTimer.Stop();
        }
    }
} 