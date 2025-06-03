using System.Collections.ObjectModel;
using System.Windows;
using WalkieDohi.Entity;

namespace WalkieDohi.UI
{
    public partial class FriendSelectWindow : Window
    {
        public Friend SelectedFriend { get; private set; }

        public FriendSelectWindow(ObservableCollection<Friend> friends)
        {
            InitializeComponent();
            FriendListBox.ItemsSource = friends;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            SelectedFriend = FriendListBox.SelectedItem as Friend;
            if (SelectedFriend != null)
            {
                DialogResult = true;
            }
        }
    }
}
