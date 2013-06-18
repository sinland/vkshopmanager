using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VkShopManager.Core.VisualHelpers
{
    class BgWorkerException : ApplicationException
    {
        public BgWorkerException(string message) : base(message)
        {

        }
    }
}
