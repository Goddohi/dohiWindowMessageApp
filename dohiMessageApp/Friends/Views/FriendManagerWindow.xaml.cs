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
using WalkieDohi.Packet.Entity;

namespace WalkieDohi.Friends.Views
{
    /// <summary>
    /// FriendManagerWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FriendManagerWindow : Window
    {
        public FriendManagerWindowViewModel viewModel = new FriendManagerWindowViewModel();
        FriendFileProvider friendFilePrvider = new FriendJsonFileHandler();
        private const string ProfileUnavailableMessage = "상대방의 프로그램이 꺼져있거나 없는 IP 입니다";
        private bool isUpdatingIpBoxes = false;
        private bool closeAfterSuccessfulAdd = false;
        private bool closeAfterSuccessfulEdit = false;
        private UserProfileEntity lookupProfile;
        private string lookupProfileIp;
        private string blockedSelfIp;
        
        public FriendManagerWindow()
        {
            InitializeComponent();
            this.DataContext = viewModel;
            viewModel.Friends = new ObservableCollection<Friend>(MainData.GetsortedFriends()); // 복사본
            UpdateAddButtonState();

        }

        public FriendManagerWindow(string suggestedName, string suggestedIp)
            : this()
        {
            closeAfterSuccessfulAdd = true;
            ApplyFriendSuggestion(suggestedName, suggestedIp);
            Loaded += FriendManagerWindow_LoadedAutoLookup;
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

        private async void FriendManagerWindow_LoadedAutoLookup(object sender, RoutedEventArgs e)
        {
            Loaded -= FriendManagerWindow_LoadedAutoLookup;
            await LookupProfileAsync(false);
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

            UpdateAddButtonState();
        }


        #region 클리어 로직
        private void AddBoxAllClear()
        {
            NameBox.Clear();
            AddBoxIpClear();
            UpdateAddButtonState();
        }

        private void AddBoxIpClear()
        {
            IpBox1.Clear(); IpBox2.Clear(); IpBox3.Clear(); IpBox4.Clear();
            UpdateAddButtonState();
        }

        #endregion

        private string GetIpFullstring() {
            return IpBox1.Text.Trim() + "." + IpBox2.Text.Trim() + "." + IpBox3.Text.Trim() + "." + IpBox4.Text.Trim();
        }



        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            string name;
            string ip;
            string validationMessage;
            if (!TryGetFriendInput(out name, out ip, out validationMessage))
            {
                SetLookupStatus(validationMessage);
                MessageBox.Show(validationMessage);
                return;
            }

            if (IsSelfTarget(ip))
            {
                MessageBox.Show("본인은 친구로 추가할 수 없습니다.");
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

            string lookupUuid = GetLookupUserUuidForIp(ip);
            bool duplicateUuid = !string.IsNullOrWhiteSpace(lookupUuid)
                && viewModel.Friends
                    .Where((f, index) => index != editIndex)
                    .Any(f => string.Equals(f.UserUuid, lookupUuid, StringComparison.OrdinalIgnoreCase));
            if (duplicateUuid)
            {
                MessageBox.Show("같은 사용자가 이미 친구로 등록되어 있습니다.");
                return;
            }

            if (isEditMode)
            {
                if (editIndex >= 0 && editIndex < viewModel.Friends.Count)
                {
                    string oldIp = viewModel.Friends[editIndex].Ip;
                    viewModel.Friends[editIndex].Name = name;
                    viewModel.Friends[editIndex].Ip = ip;
                    if (!string.IsNullOrWhiteSpace(lookupUuid))
                    {
                        viewModel.Friends[editIndex].UserUuid = lookupUuid;
                    }

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
            
            var addedFriend = new Friend { Name = name, Ip = ip, UserUuid = lookupUuid };
            viewModel.Friends.Add(addedFriend);
            SaveFriends();

            SelectedFriend = addedFriend;
            MessageBox.Show($"{addedFriend.Name}님이 추가되었습니다.");

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

        private async void LookupProfile_Click(object sender, RoutedEventArgs e)
        {
            await LookupProfileAsync(true);
        }

        private async Task LookupProfileAsync(bool showSearchingStatus)
        {
            string ip;
            if (!NetworkHelper.TryNormalizeIPv4(GetIpFullstring(), out ip))
            {
                ClearLookupProfile();
                SetLookupStatus("올바른 IP 주소를 입력하세요.");
                return;
            }

            if (showSearchingStatus)
            {
                SetLookupStatus("입력한 IP로 연결 확인 중입니다.");
            }

            btnLookupProfile.IsEnabled = false;
            try
            {
                var result = await new MessengerSender().RequestUserProfileAsync(ip);
                if (!result.Succeeded || result.Payload == null)
                {
                    ClearLookupProfile();
                    SetLookupStatus(ProfileUnavailableMessage);
                    return;
                }

                string userUuid;
                if (!TryNormalizeUuid(result.Payload.UserUuid, out userUuid))
                {
                    ClearLookupProfile();
                    SetLookupStatus(ProfileUnavailableMessage);
                    return;
                }

                if (string.Equals(userUuid, MainData.currentUser?.UserUuid, StringComparison.OrdinalIgnoreCase))
                {
                    MarkSelfTarget(ip);
                    SetLookupStatus("본인입니다. 자기 자신은 친구로 추가할 수 없습니다.");
                    return;
                }

                result.Payload.UserUuid = userUuid;
                result.Payload.Ip = ip;
                lookupProfile = result.Payload;
                lookupProfileIp = ip;
                blockedSelfIp = null;

                if (!string.IsNullOrWhiteSpace(lookupProfile.Nickname))
                {
                    NameBox.Text = lookupProfile.Nickname.Trim();
                }

                SetLookupStatus($"연결 확인됨: {lookupProfile.Nickname} / UUID: {lookupProfile.UserUuid}");
            }
            finally
            {
                btnLookupProfile.IsEnabled = true;
            }
        }

        private void ClearLookupProfileIfIpChanged()
        {
            if (lookupProfile == null && string.IsNullOrWhiteSpace(blockedSelfIp))
            {
                return;
            }

            string currentIp = GetNormalizedCurrentIp();
            bool sameLookupIp = lookupProfile != null && NetworkHelper.AreSameIPv4(currentIp, lookupProfileIp);
            bool sameSelfIp = !string.IsNullOrWhiteSpace(blockedSelfIp)
                && NetworkHelper.AreSameIPv4(currentIp, blockedSelfIp);

            if (!sameLookupIp && !sameSelfIp)
            {
                ClearLookupProfile();
                SetLookupStatus("");
            }
        }

        private void ClearLookupProfile()
        {
            lookupProfile = null;
            lookupProfileIp = null;
            blockedSelfIp = null;
        }

        private void MarkSelfTarget(string ip)
        {
            lookupProfile = null;
            lookupProfileIp = null;
            blockedSelfIp = ip;
        }

        private string GetLookupUserUuidForIp(string ip)
        {
            return lookupProfile != null && NetworkHelper.AreSameIPv4(ip, lookupProfileIp)
                ? lookupProfile.UserUuid
                : "";
        }

        private void SetLookupStatus(string message)
        {
            LookupStatusTextBlock.Text = message ?? "";
        }

        private bool TryGetFriendInput(out string name, out string ip, out string validationMessage)
        {
            name = NameBox.Text.Trim();
            ip = "";
            validationMessage = "";

            if (string.IsNullOrWhiteSpace(name))
            {
                validationMessage = "이름을 입력해주세요.";
                return false;
            }

            if (!HasAllIpParts())
            {
                validationMessage = "IP 주소를 모두 입력해주세요.";
                return false;
            }

            if (!NetworkHelper.TryNormalizeIPv4(GetIpFullstring(), out ip))
            {
                validationMessage = "올바른 IP 주소를 입력하세요.";
                return false;
            }

            return true;
        }

        private bool HasAllIpParts()
        {
            return GetIpBoxes().All(box => !string.IsNullOrWhiteSpace(box.Text));
        }

        private void UpdateAddButtonState()
        {
            if (btnAddFriend == null || NameBox == null)
            {
                return;
            }

            string normalizedIp;
            btnAddFriend.IsEnabled = !string.IsNullOrWhiteSpace(NameBox.Text)
                && HasAllIpParts()
                && NetworkHelper.TryNormalizeIPv4(GetIpFullstring(), out normalizedIp);
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateAddButtonState();
        }

        private bool IsSelfTarget(string ip)
        {
            if (IsLoopbackIp(ip)
                || NetworkHelper.AreSameIPv4(ip, NetworkHelper.GetLocalIPv4())
                || (!string.IsNullOrWhiteSpace(blockedSelfIp) && NetworkHelper.AreSameIPv4(ip, blockedSelfIp)))
            {
                return true;
            }

            string lookupUuid = GetLookupUserUuidForIp(ip);
            return !string.IsNullOrWhiteSpace(lookupUuid)
                && string.Equals(lookupUuid, MainData.currentUser?.UserUuid, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoopbackIp(string ip)
        {
            string normalizedIp;
            return NetworkHelper.TryNormalizeIPv4(ip, out normalizedIp)
                && IPAddress.TryParse(normalizedIp, out var address)
                && IPAddress.IsLoopback(address);
        }

        private string GetNormalizedCurrentIp()
        {
            string currentIp;
            return NetworkHelper.TryNormalizeIPv4(GetIpFullstring(), out currentIp)
                ? currentIp
                : "";
        }

        private static bool TryNormalizeUuid(string value, out string normalized)
        {
            normalized = "";
            Guid parsed;
            if (!Guid.TryParse(value, out parsed))
            {
                return false;
            }

            normalized = parsed.ToString("D");
            return true;
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
                UpdateAddButtonState();
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
                UpdateAddButtonState();
                return;
            }

            if (digitsOnly.Length == textBox.MaxLength)
            {
                MoveFocusToNextIpBox(textBox);
            }

            ClearLookupProfileIfIpChanged();
            UpdateAddButtonState();
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
            ClearLookupProfileIfIpChanged();
            UpdateAddButtonState();
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
            ClearLookupProfileIfIpChanged();
            UpdateAddButtonState();
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


