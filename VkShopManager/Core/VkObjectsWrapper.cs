using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VkApiNet.Vk;

namespace VkShopManager.Core
{
    class VkObjectsWrapper
    {
        public VkGroup GetVkGroupObject(int gid, int ownerId)
        {
            return new VkGroup(gid)
                {
                    Owner = new VkUser(ownerId)
                };
        }

        public VkAlbum GetVkGroupAlbum(VkGroup owningGroup, Int64 aid)
        {
            return new VkAlbum(aid)
                {
                    OwnerId = -owningGroup.Id,
                };
        }
    }
}
