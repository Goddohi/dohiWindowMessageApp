using System;
using System.Windows;
using WalkieDohi.ChattingRooms.Data;
using WalkieDohi.Packet.Messages.Entity;
using WalkieDohi.Util;
using WalkieDohi.Util.Tcp;

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

            message.Sender = MainData.GetFriendNameOrReturnOriginal(message.Sender, message.SenderIp);
            ChatListManager.UpdateChatList(message);
            MessageReceived?.Invoke(message);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
