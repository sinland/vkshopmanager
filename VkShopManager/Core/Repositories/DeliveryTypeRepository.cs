using System.Collections.Generic;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    internal interface IDeliveryTypeRepository
    {
        void Add(DeliveryType deliveryType);
        void Update(DeliveryType deliveryType);
        void Delete(DeliveryType deliveryType);

        DeliveryType GetById(int id);
    }

    internal class DeliveryTypeRepository : IDeliveryTypeRepository
    {
        private static readonly Dictionary<int, DeliveryType> s_idToDeliveryCache = new Dictionary<int, DeliveryType>(0);

        private readonly DbManger m_dbManger;

        private void AddToCache(DeliveryType p)
        {
            if (!s_idToDeliveryCache.ContainsKey(p.Id)) s_idToDeliveryCache.Add(p.Id, p);
        }
        private void UpdateInCache(DeliveryType p)
        {
            if (s_idToDeliveryCache.ContainsKey(p.Id)) s_idToDeliveryCache[p.Id] = p;
        }
        private void DeleteFromCache(DeliveryType p)
        {
            if (s_idToDeliveryCache.ContainsKey(p.Id)) s_idToDeliveryCache.Remove(p.Id);
        }

        public DeliveryTypeRepository(DbManger dbManger)
        {
            m_dbManger = dbManger;
        }

        public void Add(DeliveryType deliveryType)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Save(deliveryType);
                    t.Commit();
                    AddToCache(deliveryType);
                }
            }
        }

        public void Update(DeliveryType deliveryType)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Update(deliveryType);
                    t.Commit();
                    UpdateInCache(deliveryType);
                }
            }
        }

        public void Delete(DeliveryType deliveryType)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Delete(deliveryType);
                    t.Commit();
                    DeleteFromCache(deliveryType);
                }
            }
        }

        public DeliveryType GetById(int id)
        {
            DeliveryType delivery;
            if (s_idToDeliveryCache.ContainsKey(id))
            {
                delivery = s_idToDeliveryCache[id];
            }
            else
            {
                using (var s = m_dbManger.OpenSession())
                {
                    delivery = s.Get<DeliveryType>(id);
                    if (delivery != null) AddToCache(delivery);
                }
            }
            return delivery;
        }
    }
}