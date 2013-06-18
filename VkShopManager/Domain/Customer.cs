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
        public virtual int Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual long VkId { get; set; }
        public virtual int AccountTypeId
        {
            get { return m_accountTypeId; }
            set { m_accountTypeId = value; OnPropertyChanged("AccountTypeId"); }
        }
        public virtual string Address { get; set; }
        public virtual string Phone { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
