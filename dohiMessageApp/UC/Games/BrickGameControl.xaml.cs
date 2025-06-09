using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WalkieDohi.UC.Games
{
    public partial class BrickGameControl : UserControl
    {
        private const double PaddleWidth = 80;
        private const double PaddleHeight = 10;
        private const double BallSize = 10;
        private const int BrickRows = 4;
        private const int BrickCols = 8;
        private const double BrickWidth = 45;
        private const double BrickHeight = 20;

        private Rectangle paddle;
        private Ellipse ball;
        private List<Rectangle> bricks = new List<Rectangle>();

        private double ballX, ballY;
        private double ballDX = 3, ballDY = -3;
        private double paddleX;
        private bool isGameRunning = false;
        private int score = 0;
        private int currentRound = 1;

        public BrickGameControl()
        {
            InitializeComponent();
            Loaded += BrickGameControl_Loaded;
            CompositionTarget.Rendering += GameLoop;
        }

        private void BrickGameControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitGame();
        }

        private void InitGame()
        {
            this.Focus();
            GameCanvas.Children.Clear();
            bricks.Clear();
            score = 0;
            currentRound = 1;
            ScoreText.Text = $"점수: {score}";

            // 패들
            paddle = new Rectangle
            {
                Width = PaddleWidth,
                Height = PaddleHeight,
                Fill = Brushes.White
            };
            paddleX = (GameCanvas.Width - PaddleWidth) / 2;
            Canvas.SetLeft(paddle, paddleX);
            Canvas.SetTop(paddle, GameCanvas.Height - PaddleHeight - 10);
            GameCanvas.Children.Add(paddle);

            // 공
            ball = new Ellipse
            {
                Width = BallSize,
                Height = BallSize,
                Fill = Brushes.Red
            };
            ballX = 200;
            ballY = 300;
            Canvas.SetLeft(ball, ballX);
            Canvas.SetTop(ball, ballY);
            GameCanvas.Children.Add(ball);

            CreateBricks();
        }

        private void CreateBricks()
        {
            bricks.Clear();

            for (int row = 0; row < BrickRows; row++)
            {
                for (int col = 0; col < BrickCols; col++)
                {
                    Rectangle brick = new Rectangle
                    {
                        Width = BrickWidth - 4,
                        Height = BrickHeight - 4,
                        Fill = Brushes.Orange
                    };
                    Canvas.SetLeft(brick, col * BrickWidth + 2);
                    Canvas.SetTop(brick, row * BrickHeight + 2);
                    bricks.Add(brick);
                    GameCanvas.Children.Add(brick);
                }
            }
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (!isGameRunning) return;

            // 이동
            ballX += ballDX;
            ballY += ballDY;

            // 벽 충돌
            if (ballX <= 0 || ballX + BallSize >= GameCanvas.Width)
                ballDX = -ballDX;
            if (ballY <= 0)
                ballDY = -ballDY;

            // 바닥 충돌 → 게임 오버
            if (ballY > GameCanvas.Height)
            {
                isGameRunning = false;
                MessageBox.Show("게임 오버!");
                return;
            }

            // 패들 충돌
            if (ballY + BallSize >= GameCanvas.Height - PaddleHeight - 10 &&
                ballX + BallSize >= paddleX &&
                ballX <= paddleX + PaddleWidth)
            {
                ballDY = -Math.Abs(ballDY);
            }

            // 벽돌 충돌
            for (int i = bricks.Count - 1; i >= 0; i--)
            {
                var brick = bricks[i];
                double bx = Canvas.GetLeft(brick);
                double by = Canvas.GetTop(brick);
                if (ballX + BallSize > bx && ballX < bx + BrickWidth &&
                    ballY + BallSize > by && ballY < by + BrickHeight)
                {
                    ballDY = -ballDY;
                    GameCanvas.Children.Remove(brick);
                    bricks.RemoveAt(i);
                    score += 10;
                    ScoreText.Text = $"점수: {score}";
                    break;
                }
            }

            // 모든 벽돌 제거 → 라운드 업
            if (bricks.Count == 0)
            {
                currentRound++;
                ballDX += (ballDX > 0) ? 0.1 : -0.1;
                ballDY += (ballDY > 0) ? 0.1 : -0.1;
                CreateBricks();
                MessageBox.Show($"🎉 라운드 {currentRound} 시작! 속도 증가!");
            }

            // 위치 적용
            Canvas.SetLeft(ball, ballX);
            Canvas.SetTop(ball, ballY);
            Canvas.SetLeft(paddle, paddleX);
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isGameRunning) return;

            if (e.Key == Key.Left)
                paddleX = Math.Max(0, paddleX - 15);
            else if (e.Key == Key.Right)
                paddleX = Math.Min(GameCanvas.Width - PaddleWidth, paddleX + 15);
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            InitGame();
            isGameRunning = true;
            this.Focus(); // 키보드 포커스 다시 설정
        }

        private void LevelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string level = (LevelSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
            switch (level)
            {
                case "쉬움":
                    ballDX = 1.3; ballDY = -1.3;
                    break;
                case "보통":
                    ballDX = 2; ballDY = -2;
                    break;
                case "어려움":
                    ballDX = 2.5; ballDY = -2.5;
                    break;
            }
        }
    }
}
