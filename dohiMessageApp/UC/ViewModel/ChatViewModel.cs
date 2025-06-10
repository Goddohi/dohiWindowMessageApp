using System.Collections.ObjectModel;
using WalkieDohi.Entity;

namespace WalkieDohi.UC.ViewModel
{
    public class ChatViewModel
    {
        public ObservableCollection<ChatMessage> ChatMessages { get; set; }

        public ChatViewModel()
        {
            ChatMessages = new ObservableCollection<ChatMessage>();

        }
    }
}
