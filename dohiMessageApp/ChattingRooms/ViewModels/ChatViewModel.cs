using System.Collections.ObjectModel;
using WalkieDohi.ChattingRooms.Entity;

namespace WalkieDohi.ChattingRooms.ViewModels
{
    /// <summary>
    /// 실질적으로 채팅 데이터만 각자 가지고 있는 데이터모델
    /// </summary>
    public class ChatViewModel
    {
        public ObservableCollection<ChatMessage> ChatMessages { get; set; }

        public ChatViewModel()
        {
            ChatMessages = new ObservableCollection<ChatMessage>();

        }
    }
}
