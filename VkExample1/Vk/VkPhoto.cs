using System;

namespace VkApiNet.Vk
{
    public class VkPhoto : VkEntity
    {
        public VkPhoto(Int64 pid)
        {
            Id = pid;
        }
        public VkAlbum Album { get; set; }
        public Int64 OwnerId { get; set; }
        public string SourceUrl { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }
        public int CommentsCount { get; set; }
    }
}