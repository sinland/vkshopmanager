using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using VkShopManager.Annotations;
using VkShopManager.Domain;

namespace VkShopManager.Core.VisualHelpers
{
    /// <summary>
    /// Класс для отображения записей в детализации альбома по продукту
    /// </summary>
    public class ProductListViewItem : CustomListViewItem, INotifyPropertyChanged, ISelectable
    {
        private bool m_selected = false;
        private int m_orderedAmount;

        public int Id { get { return Source.Id; } }
        public string CodeNumber {
            get { return Source.CodeNumber ?? ""; }
        }
        public string Title { get { return Source.Title; } }
        public decimal Price { get { return Source.Price; } }
        public int MinAmount { get { return Source.MinAmount; } }
        public int OrderedAmount
        {
            get { return m_orderedAmount; }
            set
            {
                m_orderedAmount = value;
                OnPropertyChanged("OrderedAmount");
                OnPropertyChanged("Status");
            }
        }

        public string Status
        {
            get { return (OrderedAmount >= MinAmount ? "OK" : ""); }
        }

        public Product Source { get; set; }

        public ProductListViewItem(Product product)
        {
            Source = product;
            Source.PropertyChanged += SourceOnPropertyChanged;
            Source.PropertyChanged += SourcePropertyChangedEventHandler;
            OrderedAmount = 0;
        }

        private void SourceOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (String.Compare(propertyChangedEventArgs.PropertyName, "amount", true) == 0)
            {
                OnPropertyChanged("MinAmount");
            }
            if (String.Compare(propertyChangedEventArgs.PropertyName, "Title", true) == 0)
            {
                OnPropertyChanged("Title");
            }
            if (String.Compare(propertyChangedEventArgs.PropertyName, "Price", true) == 0)
            {
                OnPropertyChanged("Price");
            }
            if (String.Compare(propertyChangedEventArgs.PropertyName, "CodeNumber", true) == 0)
            {
                OnPropertyChanged("CodeNumber");
            }
        }

        private void SourcePropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
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

        public string IsSelected { get { return m_selected.ToString(); } }
        public override bool TitleSearchHit(string searchString)
        {
            return this.Title.StartsWith(searchString, StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool CodeNumberSearchHit(string searchString)
        {
            return CodeNumber.StartsWith(searchString, StringComparison.OrdinalIgnoreCase);
        }
    }
}
