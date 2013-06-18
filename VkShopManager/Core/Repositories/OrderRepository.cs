using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    internal interface IOrderRepository
    {
        void Add(Order order);
        void Update(Order order);
        void Delete(Order order);

        List<Order> GetOrdersForCustomer(Customer customer);
        List<Order> GetOrdersForAlbum(Album album);
        List<Order> GetOrdersForCustomerFromAlbum(Customer customer, Album album);

        List<Order> GetOrdersForProduct(Product product);
        long GetCountProductOrders(Product product);
        /// <summary>
        /// Получает общее количество заказанных позиций по товару
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        long GetProductTotalOrderedAmount(Product product);
    }

    class OrderRepository : IOrderRepository
    {
        private readonly DbManger m_dbManger;
        private static readonly Dictionary<int, Order> s_idToOrderCache = new Dictionary<int, Order>(0);

        public OrderRepository(DbManger dbManger)
        {
            m_dbManger = dbManger;
        }

        private void AddToCache(Order o)
        {
            if (!s_idToOrderCache.ContainsKey(o.Id)) s_idToOrderCache.Add(o.Id, o);
        }

        private void UpdateInCache(Order o)
        {
            if (s_idToOrderCache.ContainsKey(o.Id)) s_idToOrderCache[o.Id] = o;
        }
        private void DeleteFromCache(Order o)
        {
            if (s_idToOrderCache.ContainsKey(o.Id)) s_idToOrderCache.Remove(o.Id);
        }

        public void Add(Order order)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Save(order);
                    t.Commit();
                    AddToCache(order);
                }
            }
        }

        public void Update(Order order)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Update(order);
                    t.Commit();
                    UpdateInCache(order);
                }
            }
        }

        public void Delete(Order order)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Delete(order);
                    t.Commit();
                    DeleteFromCache(order);
                }
            }
        }

        public List<Order> GetOrdersForCustomer(Customer customer)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from Order as o where o.CustomerId = :oid");
                q.SetInt32("oid", customer.Id);
                return (List<Order>) q.List<Order>();
            }
        }

        public List<Order> GetOrdersForAlbum(Album album)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery productsQuery = s.CreateQuery("from Product p where p.AlbumId = :aid");
                productsQuery.SetInt32("aid", album.Id);
                var idsList = productsQuery.List<Product>().Select(product => product.Id).ToList();

                if(idsList.Count == 0) return new List<Order>(0);
                List<Order> result;
                if (idsList.Count > 0)
                {
                    IQuery q = s.CreateQuery("from Order as o where o.ProductId in (:namesList)");
                    q.SetParameterList("namesList", idsList);
                    result = (List<Order>)q.List<Order>();
                }
                else result = new List<Order>(0);
                return result;
            }
        }

        public List<Order> GetOrdersForCustomerFromAlbum(Customer customer, Album album)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery productsQuery = s.CreateQuery("from Product p where p.AlbumId = :aid");
                productsQuery.SetInt32("aid", album.Id);
                var idsList = productsQuery.List<Product>().Select(product => product.Id).ToList();
                if (idsList.Count == 0) return new List<Order>(0);

                IQuery q = s.CreateQuery("from Order as o where o.CustomerId = :oid and o.ProductId in (:namesList)");
                q.SetInt32("oid", customer.Id);
                q.SetParameterList("namesList", idsList);

                return (List<Order>)q.List<Order>();
            }
        }

        public List<Order> GetOrdersForProduct(Product product)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from Order as o where o.ProductId = :pid");
                q.SetInt32("pid", product.Id);
                return (List<Order>)q.List<Order>();
            }
        }

        public long GetCountProductOrders(Product product)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("select count(*) from Order as o where o.ProductId = :pid");
                q.SetInt32("pid", product.Id);
                var result = q.UniqueResult();
                return (long) result;
            }
        }
        public long GetProductTotalOrderedAmount(Product product)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("select sum(o.Amount) from Order as o where o.ProductId = :pid");
                q.SetInt32("pid", product.Id);
                var result = q.UniqueResult();
                return (long)result;
            }
        }
    }
}