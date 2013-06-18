using System.Collections.Generic;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    internal interface IRatesRepository
    {
        void Add(ManagedRate rate);
        void Delete(ManagedRate rate);
        void Update(ManagedRate rate);

        ManagedRate GetById(int id);
        List<ManagedRate> GetAll();
    }
    class RatesRepository : IRatesRepository
    {
        private readonly DbManger m_dbManger;
        private static readonly Dictionary<int, ManagedRate> s_idToRateCache = new Dictionary<int, ManagedRate>(0);

        public RatesRepository(DbManger dbManger)
        {
            m_dbManger = dbManger;
        }
        private void AddToCache(ManagedRate p)
        {
            if (!s_idToRateCache.ContainsKey(p.Id)) s_idToRateCache.Add(p.Id, p);
        }

        private void UpdateInCache(ManagedRate p)
        {
            if (s_idToRateCache.ContainsKey(p.Id)) s_idToRateCache[p.Id] = p;
        }
        private void DeleteFromCache(ManagedRate p)
        {
            if (s_idToRateCache.ContainsKey(p.Id)) s_idToRateCache.Remove(p.Id);
        }

        public void Add(ManagedRate rate)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {

                    s.Save(rate);
                    t.Commit();
                    AddToCache(rate);
                }
            }
        }

        public void Delete(ManagedRate rate)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {

                    s.Delete(rate);
                    t.Commit();
                    DeleteFromCache(rate);
                }
            }
        }

        public void Update(ManagedRate rate)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {

                    s.Update(rate);
                    t.Commit();
                    UpdateInCache(rate);
                }
            }
        }

        public ManagedRate GetById(int id)
        {
            ManagedRate rate;
            if (s_idToRateCache.ContainsKey(id))
            {
                rate = s_idToRateCache[id];
            }
            else
            {
                using (var s = m_dbManger.OpenSession())
                {
                    rate = s.Get<ManagedRate>(id);
                    if (rate != null) AddToCache(rate);
                }
            }
            return rate;
        }

        public List<ManagedRate> GetAll()
        {
            using (var s = m_dbManger.OpenSession())
            {
                var q = s.CreateQuery("from ManagedRate where 1 = 1");
                return (List<ManagedRate>)q.List<ManagedRate>();
            }
        }
    }
}