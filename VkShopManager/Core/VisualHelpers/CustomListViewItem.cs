using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Core.VisualHelpers
{
    abstract public class CustomListViewItem
    {
        public abstract bool TitleSearchHit(string searchString);
        public abstract bool CodeNumberSearchHit(string searchString);
    }
}
