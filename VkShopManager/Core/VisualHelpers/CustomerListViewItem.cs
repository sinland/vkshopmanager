using System;
using System.ComponentModel;
using VkShopManager.Annotations;
using VkShopManager.Domain;

namespace VkShopManager.Core.VisualHelpers
{
    /// <summary>
    /// ����� ��� ����������� ������� � ����������� ������� �� �����������
    /// </summary>
    public class CustomerListViewItem : CustomListViewItem, INotifyPropertyChanged, ISelectable
    {
        private bool m_selected = false;

        private ManagedRate m_rateInfo;
        private decimal m_payment;

        /// <summary>
        /// ����� ������� � ������
        /// </summary>
        public int OrderedItemsCount { get; set; }
        /// <summary>
        /// ���������� � ����������
        /// </summary>
        public Customer Source { get; set; }
        /// <summary>
        /// ����� ������ ��� �������
        /// </summary>
        public decimal CleanSum { get; set; }
        /// <summary>
        /// ������������� ����������
        /// </summary>
        public int Id
        {
            get { return Source.Id; }
        }
        /// <summary>
        /// ������ ������� ����������
        /// </summary>
        public string AccountType
        {
            get { return m_rateInfo.Comment; }
        }
        /// <summary>
        /// ����� ��������
        /// </summary>
        public decimal CommissionSum
        {
            get { return Math.Round(CleanSum*(m_rateInfo.Rate/100), 2); }
        }
        /// <summary>
        /// ����
        /// </summary>
        public decimal TotalSum
        {
            get { return CleanSum + CommissionSum; }
        }
        /// <summary>
        /// ��� ����������
        /// </summary>
        public string FullName
        {
            get { return Source.GetFullName(); }
        }
        /// <summary>
        /// ���������, ��� � ������ ���� �������� �������
        /// </summary>
        public bool HasPartialPosition { get; set; }
        /// <summary>
        /// ������ ������ (���� �� � ������ �������� �������)
        /// </summary>
        public string Status
        {
            get
            {
                var result = "";
                if (Payment == 0)
                {
                    result += "�� �������";
                    if (HasPartialPosition) result += ", ��������";
                }
                else if (Math.Round(Payment, 0) < Math.Round(TotalSum, 0))
                {
                    result += "�������� �������";
                    if (HasPartialPosition) result += ", ��������";
                }
                else
                {
                    result += "�������";
                    if (HasPartialPosition) result += ", ��������";
                }

                return result;
            }
        }

        public CustomerListViewItem(Customer customer)
        {
            Payment = 0;
            Source = customer;
            Source.PropertyChanged += Source_PropertyChanged;
            HasPartialPosition = false;
            OrderedItemsCount = 0;
            CleanSum = 0;
            m_rateInfo = new ManagedRate {Comment = "???", Id = 0, Rate = 0};
        }

        void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (String.Compare(e.PropertyName, "AccountTypeId", true) == 0)
            {
                // ���� �������� ������ ��� ������������. ���� ������� ���, ��� � ���� �������:
                // ��������, ����� ����� ������
                
                // ����� ��� ���� �� ������ !!!
                ManagedRate r;
                try
                {
                    r = Repositories.DbManger.GetInstance().GetRatesRepository().GetById(Source.AccountTypeId);
                }
                catch
                {
                    r = null;
                }
                if (r == null) return;
                m_rateInfo = r;

                OnPropertyChanged("AccountType");
                OnPropertyChanged("CommissionSum");
                OnPropertyChanged("TotalSum");
            }
        }
        /// <summary>
        /// ������������� �������� ��� ����������
        /// </summary>
        /// <param name="rateInfo"></param>
        public void ApplyCommission(ManagedRate rateInfo)
        {
            m_rateInfo = rateInfo;
        }

        public ManagedRate GetComissionInfo()
        {
            return m_rateInfo;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public string IsSelected {
            get { return m_selected.ToString(); }
        }

        // ����� ����� ����������� �����������
        public decimal Payment
        {
            get { return m_payment; }
            set { m_payment = value; OnPropertyChanged("Status"); }
        }

        public override bool TitleSearchHit(string searchString)
        {
            return FullName.StartsWith(searchString, StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool CodeNumberSearchHit(string searchString)
        {
            return Id.ToString().StartsWith(searchString);
        }
    }
}
