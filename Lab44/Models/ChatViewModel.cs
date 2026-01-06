// Models/ChatViewModel.cs
using System;

namespace AdvertisementServiceMVC2.Models
{
    public class ChatViewModel
    {
        public string InterlocutorId { get; set; }
        public AppUser Interlocutor { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
        public Advertisement Advertisement { get; set; }
        public int MessageCount { get; set; }

        public string FormattedDate
        {
            get
            {
                if (LastMessageDate.Date == DateTime.Today)
                    return LastMessageDate.ToString("HH:mm");
                else if (LastMessageDate.Date == DateTime.Today.AddDays(-1))
                    return "Вчера";
                else
                    return LastMessageDate.ToString("dd.MM.yy");
            }
        }
    }
}