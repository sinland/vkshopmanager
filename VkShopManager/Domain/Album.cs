using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Domain
{
    public class Album
    {
        public virtual int Id { get; set; }
        public virtual long VkId { get; set; }
        public virtual string Title { get; set; }
        public virtual DateTime CreationDate { get; set; }
        public virtual string ThumbImg { get; set; }

        public Album()
        {
        }

        public string GetCleanTitle()
        {
            var result = Title;
            if (!String.IsNullOrEmpty(Title) && Title.Length > 1)
            {
                result = Title.Substring(0, 1).ToUpper() + Title.Substring(1);
            }
            return result;
        }

        public override string ToString()
        {
            return Title.Length > 0 ? GetCleanTitle() : base.ToString();
        }
    }
}
