using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Domain
{
    public class ParsedComment
    {
        public virtual int Id { get; set; }
        public virtual Int64 VkId { get; set; }
        public virtual string UniqueKey { get; set; }
        public virtual DateTime ParsingDate { get; set; }
        public virtual string Message { get; set; }
        public virtual string SenderName { get; set; }
        public virtual string PostingDate { get; set; }

        public ParsedComment()
        {

        }

        public void SetUniqueKey(long albumId, long productVkId)
        {
            UniqueKey = String.Format("{0}-{1}-{2}", albumId, productVkId, this.VkId);
        }
    }
}
