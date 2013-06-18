using System;
using System.ComponentModel;
using VkShopManager.Annotations;
using VkShopManager.Domain;

namespace VkShopManager.Core.VisualHelpers
{
    /// <summary>
    /// Класс для отображения записей в детализации альбома по покупателям
    /// </summary>
    public class CustomerListViewItem : CustomListViewItem, INotifyPropertyChanged, ISelectable
    {
        private bool m_selected = false;

        private ManagedRate m_rateInfo;
        private decimal m_payment;

        /// <summary>
        /// Число позиций в заказе
        /// </summary>
        public int OrderedItemsCount { get; set; }
        /// <summary>
        /// Информация о покупателе
        /// </summary>
        public Customer Source { get; set; }
        /// <summary>
        /// Сумма заказа без комисии
        /// </summary>
        public decimal CleanSum { get; set; }
        /// <summary>
        /// Идентификатор покупателя
        /// </summary>
        public int Id
        {
            get { return Source.Id; }
        }
        /// <summary>
        /// Ставка комисии покупателя
        /// </summary>
        public string AccountType
        {
            get { return m_rateInfo.Comment; }
        }
        /// <summary>
        /// Сумма комиссии
        /// </summary>
        public decimal CommissionSum
        {
            get { return Math.Round(CleanSum*(m_rateInfo.Rate/100), 2); }
        }
        /// <summary>
        /// Итог
        /// </summary>
        public decimal TotalSum
        {
            get { return CleanSum + CommissionSum; }
        }
        /// <summary>
        /// Имя покупателя
        /// </summary>
        public string FullName
        {
            get { return Source.GetFullName(); }
        }
        /// <summary>
        /// Указывает, что в заказе есть неполные позиции
        /// </summary>
        public bool HasPartialPosition { get; set; }
        /// <summary>
        /// Статус заказа (есть ли в заказе неполные позиции)
        /// </summary>
        public string Status
        {
            get
            {
                var result = "";
                if (Payment == 0)
                {
                    result += "Не оплачен";
                    if (HasPartialPosition) result += ", Неполный";
                }
                else if (Math.Round(Payment, 0) < Math.Round(TotalSum, 0))
                {
                    result += "Частично оплачен";
                    if (HasPartialPosition) result += ", Неполный";
                }
                else
                {
                    result += "Оплачен";
                    if (HasPartialPosition) result += ", Неполный";
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
                // была изменена ставка для пользователя. надо сменить все, что с этим связано:
                // комиссию, общую сумма заказа
                
                // этого тут быть не должно !!!
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
        /// Устанавливает комиссию для покупателя
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

        // Число денег заплаченных покупателем
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
