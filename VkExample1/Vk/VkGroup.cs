using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkApiNet.Vk
{
    public class VkGroup : VkEntity
    {
        public VkGroup(Int64 gid)
        {
            Id = gid;
        }
        public string Name { get; set; }
        public string PhotoUrl { get; set; }
        public string ScreenName { get; set; }
        public VkUser Owner { get; set; }
        public bool IsAdmin { get; set; }
    }
}
