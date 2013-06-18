using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Core.VisualHelpers
{
    public interface ISelectable
    {
        void Select();
        void Unselect();
        string IsSelected { get; }
    }
}
