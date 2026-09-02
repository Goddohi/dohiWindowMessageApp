using WalkieDohi.ChattingRooms.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using WalkieDohi;
using System.Collections.ObjectModel;
using WalkieDohi.ChattingRooms.Data;
using WalkieDohi.Util;
using WalkieDohi.Util.IO;
using WalkieDohi.Util.Provider;
using WalkieDohi.Friends.Entity;
using WalkieDohi.Friends.ViewModels;

namespace WalkieDohi.Friends.Views
{
    /// <summary>
    /// FriendManagerWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FriendManagerWindow : Window
    {
        public FriendManagerWindowViewModel viewModel = new FriendManagerWindowViewModel();
        FriendFileProvider friendFilePrvider = new FriendJsonFileHandler();
        private bool isUpdatingIpBoxes = false;
        private bool closeAfterSuccessfulAdd = false;
        private bool closeAfterSuccessfulEdit = false;
        
        public FriendManagerWindow()
        {
            InitializeComponent();
            this.DataContext = viewModel;
            viewModel.Friends = new ObservableCollection<Friend>(MainData.GetsortedFriends()); // 복사본

        }

        public FriendManagerWindow(string suggestedName, string suggestedIp)
            : this()
        {
            closeAfterSuccessfulAdd = true;
            ApplyFriendSuggestion(suggestedName, suggestedIp);
        }

        public FriendManagerWindow(Friend editTarget)
            : this()
        {
            closeAfterSuccessfulEdit = true;
            BeginEditFriend(editTarget);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private void ApplyFriendSuggestion(string suggestedName, string suggestedIp)
        {
            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                NameBox.Text = suggestedName.Trim();
            }

            SetIpBoxes(suggestedIp);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    NameBox.Focus();
                    return;
                }

                btnAddFriend.Focus();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void BeginEditFriend(Friend editTarget)
        {
            if (editTarget == null)
            {
                return;
            }

            int targetIndex = FindFriendIndex(editTarget);
            if (targetIndex < 0)
            {
                ApplyFriendSuggestion(editTarget.Name, editTarget.Ip);
                return;
            }

            LoadFriendForEdit(viewModel.Friends[targetIndex], targetIndex);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private int FindFriendIndex(Friend target)
        {
            for (int index = 0; index < viewModel.Friends.Count; index++)
            {
                Friend friend = viewModel.Friends[index];
                bool sameUuid = !string.IsNullOrWhiteSpace(target.UserUuid)
                    && string.Equals(friend.UserUuid, target.UserUuid, StringComparison.Ordinal);
                bool sameIp = NetworkHelper.AreSameIPv4(friend.Ip, target.Ip);

                if (sameUuid || sameIp)
                {
                    return index;
                }
            }

            return -1;
        }

        private void SetIpBoxes(string ip)
        {
            string normalizedIp;
            if (!NetworkHelper.TryNormalizeIPv4(ip, out normalizedIp))
            {
                return;
            }

            var parts = normalizedIp.Split('.');
            if (parts.Length != 4)
            {
                return;
            }

            try
            {
                isUpdatingIpBoxes = true;
                IpBox1.Text = parts[0];
                IpBox2.Text = parts[1];
                IpBox3.Text = parts[2];
                IpBox4.Text = parts[3];
            }
            finally
            {
                isUpdatingIpBoxes = false;
            }
        }


        #region 클리어 로직
        private void AddBoxAllClear()
        {
            NameBox.Clear();
            AddBoxIpClear();
        }

        private void AddBoxIpClear()
        {
            IpBox1.Clear(); IpBox2.Clear(); IpBox3.Clear(); IpBox4.Clear();
        }

        #endregion

        private string GetIpFullstring() {
            return IpBox1.Text.Trim() + "." + IpBox2.Text.Trim() + "." + IpBox3.Text.Trim() + "." + IpBox4.Text.Trim();
        }



        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string ipText = GetIpFullstring();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ipText))
            {
                MessageBox.Show("모든 항목을 입력해주세요.");
                return;
            }

            string ip;
            if (!NetworkHelper.TryNormalizeIPv4(ipText, out ip))
            {
                MessageBox.Show("올바른 IP 주소를 입력하세요.");
                return;
            }

            // 자기 자신을 제외한 IP 중복 검사 (수정모드가 아닐때는 -1이므로 영향 없음)
            bool duplicateIp = viewModel.Friends
                .Where((f, index) => index != editIndex)
                .Any(f => NetworkHelper.AreSameIPv4(f.Ip, ip));
            if (duplicateIp)
            {
                MessageBox.Show("같은 IP가 이미 존재합니다.");
                return;
            }

            if (isEditMode)
            {
                if (editIndex >= 0 && editIndex < viewModel.Friends.Count)
                {
                    string oldIp = viewModel.Friends[editIndex].Ip;
                    viewModel.Friends[editIndex].Name = name;
                    viewModel.Friends[editIndex].Ip = ip;
                    SaveFriends();
                    ChatListManager.ReplaceFriendIpReferences(oldIp, ip, name);
                    MainData.NotifyFriendsChanged();

                    SelectedFriend = viewModel.Friends[editIndex];
                    if (closeAfterSuccessfulEdit)
                    {
                        DialogResult = true;
                        Close();
                        return;
                    }
                }
                ResetToAddMode();
                return;
            }      
            
            var addedFriend = new Friend { Name = name, Ip = ip };
            viewModel.Friends.Add(addedFriend);
            SaveFriends();

            SelectedFriend = addedFriend;
            if (closeAfterSuccessfulAdd)
            {
                DialogResult = true;
                Close();
                return;
            }

            AddBoxAllClear();
        }

        private void SaveFriends()
        {
            friendFilePrvider.SaveFriends(viewModel.Friends);
            ChatListManager.RefreshSingleChatNamesFromFriends();
        }

        private void IpBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            int boxIndex = GetIpBoxIndex(textBox);
            if (boxIndex < 0)
            {
                e.Handled = true;
                return;
            }

            string nextText = BuildIpBoxTextAfterInput(textBox, e.Text);

            if (nextText.Contains("."))
            {
                e.Handled = true;
                DistributeDottedIpText(boxIndex, nextText);
                return;
            }

            string digitsOnly = new string(nextText.Where(char.IsDigit).ToArray());
            if (nextText != digitsOnly)
            {
                e.Handled = true;
                return;
            }

            if (digitsOnly.Length > textBox.MaxLength)
            {
                e.Handled = true;
                DistributeIpDigits(boxIndex, digitsOnly);
                return;
            }

            e.Handled = false;
        }

        private void IpBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!(sender is TextBox textBox)
                || !e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string pastedText = e.DataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(pastedText))
            {
                e.CancelCommand();
                return;
            }

            string nextText = BuildIpBoxTextAfterInput(textBox, pastedText);
            int boxIndex = GetIpBoxIndex(textBox);
            if (boxIndex < 0)
            {
                e.CancelCommand();
                return;
            }

            e.CancelCommand();
            if (nextText.Contains("."))
            {
                DistributeDottedIpText(boxIndex, nextText);
                return;
            }

            string digitsOnly = new string(nextText.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(digitsOnly))
            {
                DistributeIpDigits(boxIndex, digitsOnly);
            }
        }

        private void IpBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingIpBoxes || !(sender is TextBox textBox) || !textBox.IsKeyboardFocusWithin)
            {
                return;
            }

            int boxIndex = GetIpBoxIndex(textBox);
            if (boxIndex < 0)
            {
                return;
            }

            if (textBox.Text.Contains("."))
            {
                DistributeDottedIpText(boxIndex, textBox.Text);
                return;
            }

            string digitsOnly = new string(textBox.Text.Where(char.IsDigit).ToArray());
            if (textBox.Text != digitsOnly)
            {
                SetIpBoxText(textBox, digitsOnly);
            }

            if (digitsOnly.Length > textBox.MaxLength)
            {
                DistributeIpDigits(boxIndex, digitsOnly);
                return;
            }

            if (digitsOnly.Length == textBox.MaxLength)
            {
                MoveFocusToNextIpBox(textBox);
            }
        }

        private string BuildIpBoxTextAfterInput(TextBox textBox, string inputText)
        {
            int selectionStart = Math.Min(textBox.SelectionStart, textBox.Text.Length);
            int selectionLength = Math.Min(textBox.SelectionLength, textBox.Text.Length - selectionStart);

            return textBox.Text
                .Remove(selectionStart, selectionLength)
                .Insert(selectionStart, inputText);
        }

        private int GetIpBoxIndex(TextBox textBox)
        {
            TextBox[] ipBoxes = GetIpBoxes();
            return Array.IndexOf(ipBoxes, textBox);
        }

        private TextBox[] GetIpBoxes()
        {
            return new[] { IpBox1, IpBox2, IpBox3, IpBox4 };
        }

        private void SetIpBoxText(TextBox textBox, string text)
        {
            try
            {
                isUpdatingIpBoxes = true;
                textBox.Text = text.Length > textBox.MaxLength
                    ? text.Substring(0, textBox.MaxLength)
                    : text;
                textBox.CaretIndex = textBox.Text.Length;
            }
            finally
            {
                isUpdatingIpBoxes = false;
            }
        }

        private void DistributeIpDigits(int startIndex, string digits)
        {
            if (startIndex < 0 || string.IsNullOrEmpty(digits))
            {
                return;
            }

            TextBox[] ipBoxes = GetIpBoxes();
            int offset = 0;
            int focusIndex = startIndex;

            try
            {
                isUpdatingIpBoxes = true;
                for (int index = startIndex; index < ipBoxes.Length && offset < digits.Length; index++)
                {
                    int length = Math.Min(ipBoxes[index].MaxLength, digits.Length - offset);
                    ipBoxes[index].Text = digits.Substring(offset, length);
                    ipBoxes[index].CaretIndex = ipBoxes[index].Text.Length;
                    offset += length;
                    focusIndex = index;
                }
            }
            finally
            {
                isUpdatingIpBoxes = false;
            }

            if (offset >= digits.Length && focusIndex < ipBoxes.Length - 1 && ipBoxes[focusIndex].Text.Length == ipBoxes[focusIndex].MaxLength)
            {
                focusIndex++;
            }

            FocusIpBoxOrSubmitButton(focusIndex);
        }

        private void DistributeDottedIpText(int startIndex, string text)
        {
            if (startIndex < 0)
            {
                return;
            }

            TextBox[] ipBoxes = GetIpBoxes();
            string[] parts = text.Split('.');
            int focusIndex = startIndex;

            try
            {
                isUpdatingIpBoxes = true;
                for (int partIndex = 0; partIndex < parts.Length && startIndex + partIndex < ipBoxes.Length; partIndex++)
                {
                    string digitsOnly = new string(parts[partIndex].Where(char.IsDigit).ToArray());
                    TextBox targetBox = ipBoxes[startIndex + partIndex];
                    targetBox.Text = digitsOnly.Length > targetBox.MaxLength
                        ? digitsOnly.Substring(0, targetBox.MaxLength)
                        : digitsOnly;
                    targetBox.CaretIndex = targetBox.Text.Length;
                    focusIndex = startIndex + partIndex;
                }
            }
            finally
            {
                isUpdatingIpBoxes = false;
            }

            if (focusIndex < ipBoxes.Length - 1 && ipBoxes[focusIndex].Text.Length == ipBoxes[focusIndex].MaxLength)
            {
                focusIndex++;
            }

            FocusIpBoxOrSubmitButton(focusIndex);
        }

        private void MoveFocusToNextIpBox(TextBox currentBox)
        {
            int boxIndex = GetIpBoxIndex(currentBox);
            if (boxIndex < 0)
            {
                return;
            }

            FocusIpBoxOrSubmitButton(boxIndex + 1);
        }

        private void FocusIpBoxOrSubmitButton(int boxIndex)
        {
            TextBox[] ipBoxes = GetIpBoxes();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsVisible)
                {
                    return;
                }

                if (boxIndex >= ipBoxes.Length)
                {
                    btnAddFriend.Focus();
                    return;
                }

                if (boxIndex < 0)
                {
                    return;
                }

                ipBoxes[boxIndex].Focus();
                ipBoxes[boxIndex].CaretIndex = ipBoxes[boxIndex].Text.Length;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void txtAddBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                e.Handled = true; // 기본 Tab 동작 막기
                if (sender is TextBox textBox)
                {
                    switch (textBox.Name)
                    {
                        case "NameBox":
                            IpBox1.Focus();
                            break;
                        case "IpBox1":
                            IpBox2.Focus();
                            break;
                        case "IpBox2":
                            IpBox3.Focus();
                            break;
                        case "IpBox3":
                            IpBox4.Focus();
                            break;
                        case "IpBox4":
                            btnAddFriend.Focus();
                            break;
                    }
                    
                }
            }
        }

        public Friend SelectedFriend { get; set; }
        /// <summary>
        /// 수정모드 
        /// </summary>
        private bool isEditMode = false;
        private int editIndex = -1;

        private void LoadFriendForEdit(Friend friend, int friendIndex)
        {
            if (friend == null || friendIndex < 0)
            {
                return;
            }

            NameBox.Text = friend.Name;
            SetIpBoxes(friend.Ip);

            isEditMode = true;
            editIndex = friendIndex;
            SetWindowModeText("친구 수정");
        }

        private void ResetToAddMode()
        {
            isEditMode = false;
            editIndex = -1;
            SetWindowModeText("친구추가");
            AddBoxAllClear();
        }

        private void SetWindowModeText(string text)
        {
            Title = text;
            TitleText.Text = text;
            btnAddFriend.Content = text;
        }
    }
}


