using System;
using System.Collections.Generic;
using NHibernate;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    internal interface IAlbumsRepository
    {
        void Add(Album album);
        void Update(Album album);
        void Delete(Album album);

        List<Album> GetAll();
        Album GetById(int id);
        Album GetByVkId(Int64 id);
    }

    class AlbumsRepository : IAlbumsRepository
    {
        private readonly DbManger m_dbManger;
        private static readonly Dictionary<int, Album> s_idToAlbumCache = new Dictionary<int, Album>(0);
        private static readonly Dictionary<Int64, Album> s_vkidToAlbumCache = new Dictionary<Int64, Album>(0);

        public AlbumsRepository(DbManger dbManger)
        {
            m_dbManger = dbManger;
        }

        private void AddToCache(Album album)
        {
            if (!s_idToAlbumCache.ContainsKey(album.Id)) s_idToAlbumCache.Add(album.Id, album);
            if (!s_vkidToAlbumCache.ContainsKey(album.VkId)) s_vkidToAlbumCache.Add(album.VkId, album);
        }

        private void UpdateInCache(Album album)
        {
            if (s_idToAlbumCache.ContainsKey(album.Id)) s_idToAlbumCache[album.Id] = album;
            if (s_vkidToAlbumCache.ContainsKey(album.VkId)) s_vkidToAlbumCache[album.VkId] = album;
        }
        private void DeleteFromCache(Album album)
        {
            if (s_idToAlbumCache.ContainsKey(album.Id)) s_idToAlbumCache.Remove(album.Id);
            if (s_vkidToAlbumCache.ContainsKey(album.VkId)) s_vkidToAlbumCache.Remove(album.VkId);
        }

        public void Add(Album album)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Save(album);
                    t.Commit();
                    AddToCache(album);
                }
            }
        }

        public void Update(Album album)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Update(album);
                    t.Commit();
                    UpdateInCache(album);
                }
            }
        }

        public void Delete(Album album)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Delete(album);
                    t.Commit();
                    DeleteFromCache(album);
                }
            }
        }

        public List<Album> GetAll()
        {
            using (var s = m_dbManger.OpenSession())
            {
                var q = s.CreateQuery("from Album where 1 = 1");
                return (List<Album>) q.List<Album>();
            }
        }

        public Album GetById(int id)
        {
            Album album;
            if (s_idToAlbumCache.ContainsKey(id))
            {
                album = s_idToAlbumCache[id];
            }
            else
            {
                using (var s = m_dbManger.OpenSession())
                {
                    album = s.Get<Album>(id);
                    if (album != null) AddToCache(album);
                }
            }
            
            return album;
        }

        public Album GetByVkId(Int64 vkid)
        {
            Album album;
            if (s_vkidToAlbumCache.ContainsKey(vkid))
            {
                album = s_vkidToAlbumCache[vkid];
            }
            else
            {
                using (var s = m_dbManger.OpenSession())
                {
                    IQuery q = s.CreateQuery("from Album as a where a.VkId = :vkid");
                    q.SetInt64("vkid", vkid);
                    var res = q.List<Album>();
                    album = res.Count > 0 ? res[0] : null;

                    if (album != null) AddToCache(album);
                }
            }

            return album;
        }
    }
}