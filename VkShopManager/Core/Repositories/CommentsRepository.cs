using System;
using System.Collections.Generic;
using NHibernate;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    internal interface ICommentsRepository
    {
        void Add(ParsedComment comment);
        bool Contains(ParsedComment comment);
        List<ParsedComment> GetForProduct(Product p);
    }

    class CommentsRepository : ICommentsRepository
    {
        private readonly DbManger m_dbManger;

        public CommentsRepository(DbManger dbManger)
        {
            m_dbManger = dbManger;
        }

        public void Add(ParsedComment comment)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Save(comment);
                    t.Commit();
                }
            }
        }

        public bool Contains(ParsedComment comment)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from ParsedComment as a where a.UniqueKey = :uk");
                q.SetString("uk", comment.UniqueKey);
                var res = q.List<ParsedComment>();
                return res.Count > 0;
            }
        }

        public List<ParsedComment> GetForProduct(Product p)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from ParsedComment as a where a.UniqueKey like :p");
                q.SetString("p", String.Format("{0}-{1}-%", p.AlbumId, p.VkId));
                var res = (List<ParsedComment>) q.List<ParsedComment>();

                // depricated stub:
                if (res.Count == 0)
                {
                    q = s.CreateQuery("from ParsedComment as a where a.UniqueKey like :p");
                    q.SetString("p", String.Format("{0}_%", p.VkId));
                    res = (List<ParsedComment>)q.List<ParsedComment>();
                }

                return res;
            }
        }
    }
}