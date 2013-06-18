using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VkShopManager.Annotations;
using VkShopManager.Domain;

namespace VkShopManager.Core.VisualHelpers
{
    class ProductCustomerListViewItem : CustomListViewItem, INotifyPropertyChanged
    {
        private readonly Customer m_customerInfo;
        private readonly Order m_orderInfo;

        public ProductCustomerListViewItem(Order orderInfo, Customer customerInfo)
        {
            m_customerInfo = customerInfo;
            m_orderInfo = orderInfo;
            m_orderInfo.PropertyChanged += OrderInfoOnPropertyChanged;
        }

        private void OrderInfoOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var propertyName = propertyChangedEventArgs.PropertyName;
            if (String.Compare(propertyName, "Amount", true) == 0) propertyName = "OrderedAmount";

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public Order SourceOrderInfo { get { return m_orderInfo; } }
        public Customer SourceCustomerInfo { get { return m_customerInfo; } }

        public string CustomerName { get { return m_customerInfo.GetFullName(); } }
        public string OrderedAmount {
            get { return String.Format("{0} шт.", m_orderInfo.Amount); }
        }
        public string Comment {
            get { return m_orderInfo.Comment; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool TitleSearchHit(string searchString)
        {
            return this.CustomerName.StartsWith(searchString, StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool CodeNumberSearchHit(string searchString)
        {
            return false;
        }
    }
}
