using System;

namespace VkApiNet.Vk
{
    public class VkComment : VkEntity
    {
        public VkComment(Int64 cid)
        {
            Id = cid;
        }
        public Int64 SenderId { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
    }
}