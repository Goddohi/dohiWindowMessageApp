using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WalkieDohi.UC.Games
{
    public partial class SnakeGameControl : UserControl
    {
        private List<Point> snake = new List<Point>();
        private Point direction = new Point(1, 0);
        private Point food;
        private int cellSize = 20;
        private int rows, cols;
        private int score = 0;
        private bool isRunning = false;

        private TimeSpan moveInterval = TimeSpan.FromMilliseconds(120);
        private Stopwatch stopwatch = new Stopwatch();
        private TimeSpan lastMoveTime = TimeSpan.Zero;

        public SnakeGameControl()
        {
            InitializeComponent();
            Loaded += SnakeGameControl_Loaded;
            CompositionTarget.Rendering += OnRendering;
        }

        private void SnakeGameControl_Loaded(object sender, RoutedEventArgs e)
        {
            rows = (int)(GameCanvas.Height / cellSize);
            cols = (int)(GameCanvas.Width / cellSize);
            InitGame();
        }

        private void InitGame()
        {
            snake.Clear();
            score = 0;
            direction = new Point(1, 0);
            snake.Add(new Point(cols / 2, rows / 2));
            SpawnFood();
            ScoreText.Text = "점수: 0";
            isRunning = false;
            lastMoveTime = TimeSpan.Zero;
            stopwatch.Reset();
            Draw();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            InitGame();
            isRunning = true;
            stopwatch.Start();
            this.Focus();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!isRunning) return;

            TimeSpan now = stopwatch.Elapsed;
            if (now - lastMoveTime < moveInterval) return;

            lastMoveTime = now;

            Point head = snake[0];
            Point newHead = new Point(head.X + direction.X, head.Y + direction.Y);

            if (newHead.X < 0 || newHead.X >= cols || newHead.Y < 0 || newHead.Y >= rows || snake.Contains(newHead))
            {
                isRunning = false;
                MessageBox.Show("게임 오버!");
                stopwatch.Stop();
                return;
            }

            snake.Insert(0, newHead);

            if (newHead == food)
            {
                score += 10;
                ScoreText.Text = $"점수: {score}";
                SpawnFood();
            }
            else
            {
                snake.RemoveAt(snake.Count - 1);
            }

            Draw();
        }

        private void SpawnFood()
        {
            Random rand = new Random();
            do
            {
                food = new Point(rand.Next(cols), rand.Next(rows));
            } while (snake.Contains(food));
        }

        private void Draw()
        {
            GameCanvas.Children.Clear();

            foreach (var segment in snake)
            {
                Rectangle rect = new Rectangle
                {
                    Width = cellSize - 2,
                    Height = cellSize - 2,
                    Fill = Brushes.LimeGreen
                };
                Canvas.SetLeft(rect, segment.X * cellSize);
                Canvas.SetTop(rect, segment.Y * cellSize);
                GameCanvas.Children.Add(rect);
            }

            Ellipse foodEllipse = new Ellipse
            {
                Width = cellSize - 2,
                Height = cellSize - 2,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(foodEllipse, food.X * cellSize);
            Canvas.SetTop(foodEllipse, food.Y * cellSize);
            GameCanvas.Children.Add(foodEllipse);
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isRunning) return;

            if (e.Key == Key.Up && direction != new Point(0, 1))
                direction = new Point(0, -1);
            else if (e.Key == Key.Down && direction != new Point(0, -1))
                direction = new Point(0, 1);
            else if (e.Key == Key.Left && direction != new Point(1, 0))
                direction = new Point(-1, 0);
            else if (e.Key == Key.Right && direction != new Point(-1, 0))
                direction = new Point(1, 0);
        }
    }
}
