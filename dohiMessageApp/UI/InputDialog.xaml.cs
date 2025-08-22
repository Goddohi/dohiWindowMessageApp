using System.Windows;
using System.Windows.Input;

namespace WalkieDohi.UI
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; }

        public InputDialog(string title, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            InputBox.Text = defaultValue;
            InputBox.SelectAll();
            InputBox.Focus();
        }
        public InputDialog(string title, string defaultValue = "",string infoValue = "")
        {
            InitializeComponent();
            Title = title;
            txtBox.Text = infoValue;
            InputBox.Text = defaultValue;
            InputBox.SelectAll();
            InputBox.Focus();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputBox.Text;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }
    }
}
