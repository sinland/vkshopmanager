using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VkShopManager.Annotations;
using VkShopManager.Domain;

namespace VkShopManager.Core.VisualHelpers
{
    class PaymentsListViewItem : CustomListViewItem, INotifyPropertyChanged, ISelectable
    {
        private readonly Payment m_payment;
        private readonly Customer m_customer;
        private bool m_selected = false;

        public int Id { get { return m_payment.Id; } }
        public string Title { get { return m_customer.GetFullName(); } }
        public string Amount { get { return m_payment.Amount.ToString("C2"); } }
        public string Date
        {
            get
            {
                return String.Format("{0:D2}.{1:D2}.{2} {3:D2}:{4:D2}:{5:D2}",
                                     m_payment.Date.Day, m_payment.Date.Month, m_payment.Date.Year, m_payment.Date.Hour,
                                     m_payment.Date.Minute, m_payment.Date.Second);
            }
        }

        public string Comment
        {
            get { return m_payment.Comment; }
        }

        public PaymentsListViewItem(Payment payment, Customer customer)
        {
            m_payment = payment;
            m_customer = customer;
        }

        public override bool TitleSearchHit(string searchString)
        {
            return Title.StartsWith(searchString, StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool CodeNumberSearchHit(string searchString)
        {
            return Id.ToString().StartsWith(searchString);
        }

        public Payment SourcePayment { get { return m_payment; } }

        public void Select()
        {
            m_selected = true;
           OnPropertyChanged("IsSelected");
        }

        public void Unselect()
        {
            m_selected = false;
            OnPropertyChanged("IsSelected");
        }

        public string IsSelected { get { return m_selected.ToString(); } }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
