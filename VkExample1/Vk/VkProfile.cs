using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkApiNet.Vk
{
    public class VkProfile
    {
        public enum EntryType
        {
            Nickname,
            ScreenName,
            Sex,
            // ----
            City,
            Country,
            Timezone,
            Photo,
            PhotoMedium,
            // ----
            HasMobile,
            Rate,
            Contacts,
            Education,
            Online
        }
        public static string EntryTypeToString(EntryType tp)
        {
            switch (tp)
            {
                case EntryType.Nickname:
                    return "nickname";
                case EntryType.ScreenName:
                    return "screen_name";
                case EntryType.Sex:
                    return "sex";
                case EntryType.City:
                    return "city";
                case EntryType.Country:
                    return "country";
                case EntryType.Timezone:
                    return "timezone";
                case EntryType.Photo:
                    return "photo";
                case EntryType.PhotoMedium:
                    return "photo_medium";
                case EntryType.HasMobile:
                    return "has_mobile";
                case EntryType.Rate:
                    return "rate";
                case EntryType.Contacts:
                    return "contacts";
                case EntryType.Education:
                    return "education";
                case EntryType.Online:
                    return "online";
                default:
                    return String.Format("{0}", tp);
            }
        }
    }
}
