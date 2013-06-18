using System.Collections.Generic;
using NHibernate;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    public interface IPaymentsRepository
    {
        void Add(Payment payment);

        List<Payment> AllForCustomer(Customer customer);
        List<Payment> AllForAlbum(Album album);
        List<Payment> AllForCustomerInAlbum(Customer customer, Album album);
    }

    class PaymentsRepository : IPaymentsRepository
    {
        private readonly DbManger m_dbManger;

        public PaymentsRepository(DbManger dbManger)
        {
            m_dbManger = dbManger;
        }

        public void Add(Payment payment)
        {
            using (var s = m_dbManger.OpenSession())
            {
                using (var t = s.BeginTransaction())
                {
                    s.Save(payment);
                    t.Commit();
                }
            }
        }

        public List<Payment> AllForCustomer(Customer customer)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from Payment as p where p.PayerId = :pid");
                q.SetInt32("pid", customer.Id);
                return (List<Payment>)q.List<Payment>();
            }
        }

        public List<Payment> AllForAlbum(Album album)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from Payment as p where p.AlbumId = :aid");
                q.SetInt32("aid", album.Id);
                return (List<Payment>)q.List<Payment>();
            }
        }

        public List<Payment> AllForCustomerInAlbum(Customer customer, Album album)
        {
            using (var s = m_dbManger.OpenSession())
            {
                IQuery q = s.CreateQuery("from Payment as p where p.AlbumId = :aid and p.PayerId = :pid");
                q.SetInt32("aid", album.Id);
                q.SetInt32("pid", customer.Id);
                return (List<Payment>)q.List<Payment>();
            }
        }
    }
}