using System;
using System.Collections.Generic;
using NHibernate;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    internal interface IProductRepository
    {
        void Add(Product product);
        void Update(Product product);
        void Delete(Product product);

        Product GetById(int id);
        List<Product> GetByVkId(Int64 vkid);
        List<Product> GetAllFromAlbum(Album album);
        List<Product> GetByCodeNumber(string code);
    }

    class ProductRepository : IProductRepository
    {
        private readonly DbManger m_dbManger;
        private static readonly Dictionary<int, Product> s_idToProductCache = new Dictionary<int, Product>(0);
        private static readonly Dictionary<Int64, Product> s_vkidToProductCache = new Dictionary<Int64, Product>(0);

        public ProductRepository(DbManger dbManger)
        {
            m_dbManger = dbManger;
        }

        private void AddToCache(Product p)
        {
            if (!s_idToProductCache.ContainsKey(p.Id)) s_idToProductCache.Add(p.Id, p);
            if (!s_vkidToProductCache.ContainsKey(p.VkId)) s_vkidToProductCache.Add(p.VkId, p);
        }

        private void UpdateInCache(Product p)
        {
            if (s_idToProductCache.ContainsKey(p.Id)) s_idToProductCache[p.Id] = p;
            if (s_vkidToProductCache.ContainsKey(p.VkId)) s_vkidToProductCache[p.VkId] = p;
        }
        private void DeleteFromCache(Product p)
        {
            if (s_idToProductCache.ContainsKey(p.Id)) s_idToProductCache.Remove(p.Id);
            if (s_vkidToProductCache.ContainsKey(p.VkId)) s_vkidToProductCache.Remove(p.VkId);
        }

        public void Add(Product product)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Save(product);
                    t.Commit();
                    AddToCache(product);
                }
            }
        }

        public void Update(Product product)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Update(product);
                    t.Commit();
                    UpdateInCache(product);
                }
            }
        }

        public void Delete(Product product)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Delete(product);
                    t.Commit();
                    DeleteFromCache(product);
                }
            }
        }

        public Product GetById(int id)
        {
            Product p;
            if (s_idToProductCache.ContainsKey(id))
            {
                p = s_idToProductCache[id];
            }
            else
            {
                using (var s = m_dbManger.OpenSession())
                {
                    p = s.Get<Product>(id);
                    if(p!= null) s_idToProductCache.Add(id, p);
                }
            }
            return p;
        }

        public List<Product> GetByVkId(long vkid)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from Product as a where a.VkId = :vkid");
                q.SetInt64("vkid", vkid);
                return (List<Product>)q.List<Product>();
            }
        }

        public List<Product> GetAllFromAlbum(Album album)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from Product as a where a.AlbumId = :aid");
                q.SetInt32("aid", album.Id);
                return  (List<Product>) q.List<Product>();
            }
        }

        public List<Product> GetByCodeNumber(string code)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from Product as a where a.CodeNumber = :aid");
                q.SetString("aid", code);
                return (List<Product>)q.List<Product>();
            }
        }
    }
}