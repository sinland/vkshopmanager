using System;
using System.Collections.Generic;
using NHibernate;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    internal interface ICustomersRepository
    {
        void Add(Customer c);
        void Update(Customer c);

        List<Customer> All();
        Customer GetById(int id);
        Customer GetByVkId(Int64 id);
        void Delete(Customer customer);
    }

    internal class CustomersRepository : ICustomersRepository
    {
        private readonly DbManger m_dbManger;
        private static readonly Dictionary<int, Customer> s_idToCustomerCache = new Dictionary<int, Customer>(0);
        private static readonly Dictionary<Int64, Customer> s_vkidToCustomerCache = new Dictionary<Int64, Customer>(0);

        public CustomersRepository(DbManger dbManger)
        {
            m_dbManger = dbManger;
        }

        private void AddToCache(Customer c)
        {
            if (!s_idToCustomerCache.ContainsKey(c.Id)) s_idToCustomerCache.Add(c.Id, c);
            if (!s_vkidToCustomerCache.ContainsKey(c.VkId)) s_vkidToCustomerCache.Add(c.VkId, c);
        }

        private void UpdateInCache(Customer c)
        {
            if (s_idToCustomerCache.ContainsKey(c.Id)) s_idToCustomerCache[c.Id] = c;
            if (s_vkidToCustomerCache.ContainsKey(c.VkId)) s_vkidToCustomerCache[c.VkId] = c;
        }
        private void DeleteFromCache(Customer c)
        {
            if (s_idToCustomerCache.ContainsKey(c.Id)) s_idToCustomerCache.Remove(c.Id);
            if (s_vkidToCustomerCache.ContainsKey(c.VkId)) s_vkidToCustomerCache.Remove(c.VkId);
        }

        public void Add(Customer c)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Save(c);
                    t.Commit();
                    AddToCache(c);
                }
            }
        }

        public List<Customer> All()
        {
            using (var s = m_dbManger.OpenSession())
            {
                var q = s.CreateQuery("from Customer where 1 = 1");
                return (List<Customer>)q.List<Customer>();
            }
        }

        public Customer GetById(int id)
        {
            Customer c;
            if (s_idToCustomerCache.ContainsKey(id))
            {
                c = s_idToCustomerCache[id];
            }
            else
            {
                using (var s = m_dbManger.OpenSession())
                {
                    c = s.Get<Customer>(id);
                    if(c != null) s_idToCustomerCache.Add(id, c);
                }
            }

            return c;
        }

        public Customer GetByVkId(Int64 vkid)
        {
            Customer c;
            if (s_vkidToCustomerCache.ContainsKey(vkid))
            {
                c = s_vkidToCustomerCache[vkid];
            }
            else
            {
                using (var s = m_dbManger.OpenSession())
                {
                    IQuery q = s.CreateQuery("from Customer as a where a.VkId = :vkid");
                    q.SetInt64("vkid", vkid);
                    var res = q.List<Customer>();
                    c = res.Count > 0 ? res[0] : null;
                    if (c != null) { s_vkidToCustomerCache.Add(vkid, c);}
                }
            }
            
            return c;
        }

        public void Delete(Customer customer)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Delete(customer);
                    t.Commit();
                    DeleteFromCache(customer);
                }
            }
        }

        public void Update(Customer c)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Update(c);
                    t.Commit();
                    UpdateInCache(c);
                }
            }
        }
    }
}