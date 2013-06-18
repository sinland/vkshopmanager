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

        private static IDeliveryTypeRepository s_deliveryTypeRepository;
        private static IAlbumsRepository s_albumsRepository;
        private static IProductRepository s_productRepository;
        private static IOrderRepository s_orderRepository;
        private static ICommentsRepository s_commentsRepository;
        private static IRatesRepository s_ratesRepository;
        private static IPaymentsRepository s_paymentsRepository;

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
            return s_albumsRepository ?? (s_albumsRepository = new AlbumsRepository(this));
        }
        public IProductRepository GetProductRepository()
        {
            return s_productRepository ?? (s_productRepository = new ProductRepository(this));
        }
        public IOrderRepository GetOrderRepository()
        {
            return s_orderRepository ?? (s_orderRepository = new OrderRepository(this));
        }
        public ICommentsRepository GetCommentsRepository()
        {
            return s_commentsRepository ?? (s_commentsRepository = new CommentsRepository(this));
        }
        public IRatesRepository GetRatesRepository()
        {
            return s_ratesRepository ?? (s_ratesRepository = new RatesRepository(this));
        }
        public IPaymentsRepository GetPaymentsRepository()
        {
            return s_paymentsRepository ?? (s_paymentsRepository = new PaymentsRepository(this));
        }
        public IDeliveryTypeRepository GetDeliveryRepository()
        {
            return s_deliveryTypeRepository ?? (s_deliveryTypeRepository = new DeliveryTypeRepository(this));
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
