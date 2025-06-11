using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WalkieDohi.UC.Games
{
    public partial class ShootingGameControl : UserControl
    {
        private DispatcherTimer gameTimer;
        private DispatcherTimer countdownTimer;
        private Random rand = new Random();
        private int score = 0;
        private int timeLeft = 30; // 30초 제한

        public ShootingGameControl()
        {
            InitializeComponent();
        }

        private void StartGame()
        {
            score = 0;
            timeLeft = 30;
            ScoreText.Text = "0";
            TimeText.Text = timeLeft.ToString();
            GameOverText.Visibility = Visibility.Collapsed;
            StartButton.Visibility = Visibility.Collapsed;
            ResultText.Visibility = Visibility.Collapsed;
            GameCanvas.Children.Clear();

            // 타겟 생성 타이머
            gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            gameTimer.Tick += GameTimer_Tick;
            gameTimer.Start();

            // 카운트다운 타이머
            countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            timeLeft--;
            TimeText.Text = timeLeft.ToString();

            if (timeLeft <= 0)
            {
                EndGame();
            }
        }

        private void EndGame()
        {
            gameTimer.Stop();
            countdownTimer.Stop();
            GameOverText.Visibility = Visibility.Visible;
            StartButton.Visibility = Visibility.Visible;
            ResultText.Text = $"최종 점수: {score}점";
            ResultText.Visibility = Visibility.Visible;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            CreateTarget();
        }

        private void CreateTarget()
        {
            if (GameCanvas.ActualWidth < 50 || GameCanvas.ActualHeight < 50)
                return;
            double targetsize = rand.Next(25, 45);
            Ellipse target = new Ellipse
            {
                Width = targetsize,
                Height = targetsize,
                Fill = Brushes.Red,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Tag = "target"
            };

            double x = rand.Next(0, (int)(GameCanvas.ActualWidth - 50));
            double y = rand.Next(0, (int)(GameCanvas.ActualHeight - 50));

            Canvas.SetLeft(target, x);
            Canvas.SetTop(target, y);
            
            target.MouseDown += Target_MouseDown;
            GameCanvas.Children.Add(target);

            double removetime = rand.NextDouble() + 0.5;
            // 제거타이머
            var removeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(removetime)
            };
            removeTimer.Tick += (s, e) =>
            {
                removeTimer.Stop();
                GameCanvas.Children.Remove(target);
            };
            removeTimer.Start();
        }

        private void Target_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Ellipse target)
            {
                GameCanvas.Children.Remove(target);
                score++;
                ScoreText.Text = score.ToString();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }
    }
}
