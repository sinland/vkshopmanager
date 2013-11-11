using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Domain
{
    public class ManagedRate
    {
        public virtual int Id { get; set; }
        public virtual string Comment { get; set; }
        /// <summary>
        /// Значние комисии в целых долях (10%)
        /// </summary>
        public virtual decimal Rate { get; set; }

        public ManagedRate()
        {
            
        }

        public override string ToString()
        {
            return Comment.Length > 0 ? Comment : base.ToString();
        }
    }
}
