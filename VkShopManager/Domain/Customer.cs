using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VkApiNet.Vk;
using VkShopManager.Annotations;

namespace VkShopManager.Domain
{
    public class Customer : INotifyPropertyChanged
    {
        private int m_accountTypeId;
        private DeliveryType m_deliveryType;
        private ManagedRate m_comissionRate;

        private int m_deliveryTypeId;

        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual long VkId { get; set; }
        public virtual int AccountTypeId
        {
            get { return m_accountTypeId; }
            set
            {
                m_accountTypeId = value;
                m_comissionRate = null;
                OnPropertyChanged("AccountTypeId");
            }
        }
        public virtual string Address { get; set; }
        public virtual string Phone { get; set; }
        public virtual int DeliveryTypeId
        {
            get { return m_deliveryTypeId; }
            set
            {
                m_deliveryTypeId = value;
                m_deliveryType = null;
            }
        }

        public Customer()
        {
            
        }

        public string GetFullName()
        {
            return String.Format("{0} {1}", LastName, FirstName).Trim();
        }
        public override string ToString()
        {
            return GetFullName();
        }
        public DeliveryType GetDeliveryInfo()
        {
            if (m_deliveryType == null)
            {
                var mgr = Core.Repositories.DbManger.GetInstance();
                var repo = mgr.GetDeliveryRepository();

                m_deliveryType = repo.GetById(this.DeliveryTypeId);
            }

            return m_deliveryType;
        }
        public ManagedRate GetCommissionInfo()
        {
            if (m_comissionRate == null)
            {
                var mgr = Core.Repositories.DbManger.GetInstance();
                var repo = mgr.GetRatesRepository();
                m_comissionRate = repo.GetById(AccountTypeId);
            }

            return m_comissionRate;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
