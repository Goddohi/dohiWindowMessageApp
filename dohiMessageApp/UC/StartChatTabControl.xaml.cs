﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WalkieDohi.Entity;

namespace WalkieDohi.UC
{
    /// <summary>
    /// StartChatTabControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class StartChatTabControl : UserControl
    {
        public event Action<Friend> OnStartChat;

        public StartChatTabControl()
        {
            InitializeComponent();
            StartChatButton.Click += StartChatButton_Click;
        }

        public void SetFriends(List<Friend> friends)
        {
            FriendComboBox.ItemsSource = friends;
            if (friends.Count > 0)
                FriendComboBox.SelectedIndex = 0;
        }

        private void StartChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (FriendComboBox.SelectedItem is Friend selected)
            {
                OnStartChat?.Invoke(selected);
            }
            else
            {
                MessageBox.Show("친구를 선택하세요.");
            }
        }
    }
}