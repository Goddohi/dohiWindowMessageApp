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
            ApplyCanonicalSenderIp(message);
            message.Group?.RefreshMemberIdentitiesFromFriends();
            message.Sender = MainData.GetFriendNameOrReturnOriginal(message.Sender, message.SenderIp, message.SenderUserUuid);
            ChatListManager.UpdateChatList(message);
            MessageReceived?.Invoke(message);
        }

        private void SaveFriendUuidIfKnown(MessageEntity message)
        {
            bool changed = MainData.TryAttachFriendUuidByIp(message.SenderIp, message.SenderUserUuid);

            message.Group?.UpsertMemberIdentity(message.SenderIp, message.SenderUserUuid);
            if (message.Group?.Members != null)
            {
                foreach (var member in message.Group.Members)
                {
                    changed |= MainData.TryAttachFriendUuidByIp(member.Ip, member.UserUuid);
                }
            }

            if (changed)
            {
                new FriendJsonFileHandler().SaveFriends(MainData.Friends);
            }
        }

        private static void ApplyCanonicalSenderIp(MessageEntity message)
        {
            string resolvedIp = MainData.ResolveIncomingSingleChatIp(message.SenderIp, message.SenderUserUuid);
            if (NetworkHelper.TryNormalizeIPv4(resolvedIp, out string normalizedIp))
            {
                message.SenderIp = normalizedIp;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
