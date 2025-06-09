using System;
using System.Windows;
using System.Windows.Controls;

namespace WalkieDohi.UC.Games
{
    public partial class NumberGuessGameControl : UserControl
    {
        private int targetNumber;
        private bool success = false;

        public NumberGuessGameControl()
        {
            InitializeComponent();
            targetNumber = new Random().Next(1, 101);
        }

        private void GuessButton_Click(object sender, RoutedEventArgs e)
        {
            if (success)
            {
                GameReset();
                return;
            }
            GuseeLogic();
        }

        private void GuseeLogic()
        {
            if (success) return;

            if (int.TryParse(InputBox.Text, out int guess))
            {
                if (guess < targetNumber)
                    ResultText.Text = "더 높아요!";
                else if (guess > targetNumber)
                    ResultText.Text = "더 낮아요!";
                else
                {
                    ResultText.Text = "정답입니다! 🎉";
                    success = true;

                    GuessButton.Content = "다시 시작하기";
                }
            }
            else
            {
                ResultText.Text = "숫자를 입력해주세요.";
            }
        }

        private void GameReset()
        {
            GuessButton.Content = "확인";
            targetNumber = new Random().Next(1, 101);
            ResultText.Text = "";
            InputBox.Text = "";
            success = false;
        }

        private void InputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key != System.Windows.Input.Key.Enter)
            {
                return;
            }
            GuseeLogic();
            e.Handled = true;

        }
    }
}
