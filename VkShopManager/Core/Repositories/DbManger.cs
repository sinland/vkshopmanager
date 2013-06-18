using System;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using VkShopManager.Domain;

namespace VkShopManager.Core.Repositories
{
    internal class DbManger
    {
        private static ISessionFactory s_session;
        private static Configuration s_cfg;
        private static DbManger s_instance;

        private DbManger()
        {
            s_cfg = new Configuration();
            s_cfg.Configure();
            s_cfg.AddAssembly(typeof (Album).Assembly);

            s_session = s_cfg.BuildSessionFactory();
        }

        public static DbManger GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = new DbManger();
            }

            return s_instance;
        }
        public ICustomersRepository GetCustomersRepository()
        {
            var u = new CustomersRepository(this);
            return u;

        }
        public IAlbumsRepository GetAlbumsRepository()
        {
            return new AlbumsRepository(this);
        }
        public IProductRepository GetProductRepository()
        {
            return new ProductRepository(this);
        }
        public IOrderRepository GetOrderRepository()
        {
            return new OrderRepository(this);
        }
        public ICommentsRepository GetCommentsRepository()
        {
            return new CommentsRepository(this);
        }
        public IRatesRepository GetRatesRepository()
        {
            return new RatesRepository(this);
        }
        public IPaymentsRepository GetPaymentsRepository()
        {
            return new PaymentsRepository(this);
        }
        public IDeliveryTypeRepository GetDeliveryRepository()
        {
            return new DeliveryTypeRepository(this);
        }
        public ISession OpenSession()
        {
            return s_session.OpenSession();
        }

        public void SetupContext()
        {
            new SchemaExport(s_cfg).Execute(false, true, false);

            s_cfg = new Configuration();
            s_cfg.Configure();
            s_cfg.AddAssembly(typeof(Album).Assembly);
            s_session = s_cfg.BuildSessionFactory();
        }
    }
}
