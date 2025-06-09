using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WalkieDohi.UC.Games
{
    public partial class LadderGameControl : UserControl
    {
        private List<string> menus;
        private Ladder ladder;
        private int columnWidth = 100;
        private int rowHeight = 20;
        private int rowCount = 12;
        private int currentRow = 0;
        private int currentCol;
        private Ellipse marker;
        private DispatcherTimer timer;

        public LadderGameControl()
        {
            InitializeComponent();
        }

        private void RunLadder_Click(object sender, RoutedEventArgs e)
        {
            LadderCanvas.Children.Clear();
            ResultText.Text = "";

            menus = MenusInput.Text.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            if (menus.Count < 2)
            {
                MessageBox.Show("메뉴를 2개 이상 입력해주세요!");
                return;
            }

            ladder = new Ladder(menus.Count, rowCount);
            DrawLadder();

            var rand = new Random();
            currentCol = rand.Next(menus.Count);
            currentRow = 0;

            StartAnimation();
        }

        private void DrawLadder()
        {
            for (int col = 0; col < ladder.ColumnCount; col++)
            {
                double x = col * columnWidth + columnWidth / 2;
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = rowCount * rowHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                LadderCanvas.Children.Add(line);
            }

            for (int row = 0; row < ladder.RowCount; row++)
            {
                for (int col = 0; col < ladder.ColumnCount - 1; col++)
                {
                    if (ladder.HorizontalLines[row, col])
                    {
                        double x1 = col * columnWidth + columnWidth / 2;
                        double x2 = (col + 1) * columnWidth + columnWidth / 2;
                        double y = row * rowHeight + rowHeight / 2;

                        var hLine = new Line
                        {
                            X1 = x1,
                            Y1 = y,
                            X2 = x2,
                            Y2 = y,
                            Stroke = Brushes.Gray,
                            StrokeThickness = 2
                        };
                        LadderCanvas.Children.Add(hLine);
                    }
                }
            }

            for (int i = 0; i < menus.Count; i++)
            {
                var label = new TextBlock
                {
                    Text = menus[i],
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkBlue
                };
                Canvas.SetLeft(label, i * columnWidth + 10);
                Canvas.SetTop(label, rowCount * rowHeight + 5);
                LadderCanvas.Children.Add(label);
            }
        }

        private void StartAnimation()
        {
            double startX = currentCol * columnWidth + columnWidth / 2;
            marker = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.Red
            };
            LadderCanvas.Children.Add(marker);
            Canvas.SetLeft(marker, startX - 8);
            Canvas.SetTop(marker, -8);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += MoveMarker;
            timer.Start();
        }

        private void MoveMarker(object sender, EventArgs e)
        {
            if (currentRow >= rowCount)
            {
                timer.Stop();
                ResultText.Text = $"🎉 오늘의 점심은 👉 {menus[currentCol]}!";
                return;
            }

            if (currentCol < ladder.ColumnCount - 1 && ladder.HorizontalLines[currentRow, currentCol])
                currentCol++;
            else if (currentCol > 0 && ladder.HorizontalLines[currentRow, currentCol - 1])
                currentCol--;

            currentRow++;

            double x = currentCol * columnWidth + columnWidth / 2;
            double y = currentRow * rowHeight - 8;
            Canvas.SetLeft(marker, x - 8);
            Canvas.SetTop(marker, y);
        }
    }

    public class Ladder
    {
        public int ColumnCount { get; }
        public int RowCount { get; }
        public bool[,] HorizontalLines { get; }

        public Ladder(int columnCount, int rowCount)
        {
            ColumnCount = columnCount;
            RowCount = rowCount;
            HorizontalLines = new bool[RowCount, ColumnCount - 1];
            GenerateRandomLines();
        }

        private void GenerateRandomLines()
        {
            var rand = new Random();
            for (int row = 0; row < RowCount; row++)
            {
                for (int col = 0; col < ColumnCount - 1; col++)
                {
                    if (!HasAdjacentLine(row, col) && rand.NextDouble() < 0.3)
                        HorizontalLines[row, col] = true;
                }
            }
        }

        private bool HasAdjacentLine(int row, int col)
        {
            if (col > 0 && HorizontalLines[row, col - 1]) return true;
            if (col < ColumnCount - 2 && HorizontalLines[row, col + 1]) return true;
            return false;
        }
    }
}
