using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VkShopManager.Annotations;
using VkShopManager.Domain;

namespace VkShopManager.Core.VisualHelpers
{
    class OrderListViewItem : CustomListViewItem, INotifyPropertyChanged
    {
        private readonly Order m_orderInfo;
        private readonly Product m_productInfo;
        private readonly ManagedRate m_comission;

        public string Title
        {
            get { return m_productInfo.Title; }
        }
        public int Amount
        {
            get { return m_orderInfo.Amount; }
        }
        public decimal Sum
        {
            get { return m_orderInfo.Amount*m_productInfo.Price; }
        }
        public decimal Comission
        {
            get { return Math.Round(Sum*(m_comission.Rate/100), 2); }
        }
        public decimal FinalSum
        {
            get { return Sum + Comission; }
        }
        public Order SourceOrder
        {
            get { return m_orderInfo; }
        }
        public Product SourceProduct { get { return m_productInfo; } }
        
        public string Comment { get { return m_orderInfo.Comment; } }
        
        public string Status { get; set; }

        public OrderListViewItem(Order orderInfo, ManagedRate comission)
        {
            m_orderInfo = orderInfo;
            m_productInfo = orderInfo.GetOrderedProduct();
            m_comission = comission;

            m_orderInfo.PropertyChanged += OrderInfoOnPropertyChanged;
        }

        private void OrderInfoOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "Amount")
            {
                OnPropertyChanged("Amount");
                OnPropertyChanged("Sum");
                OnPropertyChanged("Comission");
                OnPropertyChanged("FinalSum");
            }
            else
            {
                OnPropertyChanged(propertyChangedEventArgs.PropertyName);   
            }
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
            return this.Title.StartsWith(searchString, StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool CodeNumberSearchHit(string searchString)
        {
            return false;
        }
    }
}
