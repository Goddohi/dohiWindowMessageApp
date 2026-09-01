using System;
using System.Linq;
using System.Windows;
using WalkieDohi.ChattingRooms.Data;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.Tcp;
using WalkieDohi.Util.IO;

namespace WalkieDohi.Core.app
{
    public class MessageReceiverService : IDisposable
    {
        private PacketReceiver _receiver;
        private bool _isRunning;

        public event Action<MessageEntity> MessageReceived;

        public void Start()
        {
            if (_isRunning)
                return;

            _receiver = new PacketReceiver(MainData.GetPort());
            _receiver.OnMessageReceived += OnPacketMessageReceived;
            _receiver.Start();
            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            if (_receiver != null)
            {
                _receiver.OnMessageReceived -= OnPacketMessageReceived;
                _receiver.Stop();
                _receiver = null;
            }

            _isRunning = false;
        }

        private void OnPacketMessageReceived(MessageEntity message)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => ProcessMessage(message)));
                return;
            }

            ProcessMessage(message);
        }

        private void ProcessMessage(MessageEntity message)
        {
            if (message == null)
                return;

            SaveFriendUuidIfKnown(message);
            message.Sender = MainData.GetFriendNameOrReturnOriginal(message.Sender, message.SenderIp);
            ChatListManager.UpdateChatList(message);
            MessageReceived?.Invoke(message);
        }

        private void SaveFriendUuidIfKnown(MessageEntity message)
        {
            if (string.IsNullOrWhiteSpace(message.SenderUserUuid)
                || string.IsNullOrWhiteSpace(message.SenderIp)
                || string.Equals(message.SenderUserUuid, MainData.currentUser?.UserUuid, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!Guid.TryParse(message.SenderUserUuid, out Guid senderUuid))
            {
                return;
            }

            var normalizedUuid = senderUuid.ToString("D");
            var friend = MainData.Friends.FirstOrDefault(f =>
                string.Equals(f.UserUuid, normalizedUuid, StringComparison.OrdinalIgnoreCase));

            if (friend == null)
            {
                friend = MainData.Friends.FirstOrDefault(f =>
                    string.Equals(f.Ip, message.SenderIp, StringComparison.OrdinalIgnoreCase));
            }

            if (friend == null)
            {
                return;
            }

            bool changed = false;
            if (!string.Equals(friend.UserUuid, normalizedUuid, StringComparison.OrdinalIgnoreCase))
            {
                friend.UserUuid = normalizedUuid;
                changed = true;
            }

            if (!string.Equals(friend.Ip, message.SenderIp, StringComparison.OrdinalIgnoreCase))
            {
                friend.Ip = message.SenderIp;
                changed = true;
            }

            if (changed)
            {
                new FriendJsonFileHandler().SaveFriends(MainData.Friends);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
