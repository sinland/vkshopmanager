using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace UsersCollector.Domain
{
    internal class DbManger
    {
        private static ISessionFactory s_session;
        private static Configuration s_cfg;
        private static DbManger s_instance;

        private DbManger()
        {
            Initialize();
        }
        private void Initialize()
        {
            s_cfg = new Configuration();
            s_cfg.Configure();
            s_cfg.AddAssembly(typeof(User).Assembly);

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
        public ISession OpenSession()
        {
            return s_session.OpenSession();
        }
        public void SetupContext()
        {
            new SchemaExport(s_cfg).Execute(false, true, false);
            Initialize();
        }
    }
}
