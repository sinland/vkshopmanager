using System;

namespace VkApiNet.Vk
{
    public class VkAlbum : VkEntity
    {
        public VkAlbum(Int64 aid)
        {
            Id = aid;
        }
        public Int64 ThumbId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public int Size { get; set; }
        public string ThumbUrl { get; set; }
        public Int64 OwnerId { get; set; }
    }
}