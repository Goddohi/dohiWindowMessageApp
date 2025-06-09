using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WalkieDohi.Util;

namespace WalkieDohi.UI
{
    /// <summary>
    /// SettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("설정이 저장되었습니다.");
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private int clickCount = 0;
        private void HiddenTrigger_MouseDown(object sender, MouseButtonEventArgs e)
        {
            clickCount++;
            if (clickCount >= 5)
            {
                var gameWindow = new MiniGameWindow();
                gameWindow.Owner = this;
                gameWindow.ShowDialog();
                clickCount = 0;
            }
        }
    }
}
