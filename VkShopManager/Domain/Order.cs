using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VkShopManager.Annotations;

namespace VkShopManager.Domain
{
    public class Order : INotifyPropertyChanged
    {
        private int m_amount;
        private Product m_cachedProduct;

        /// <summary>
        /// ИД в БД
        /// </summary>
        public virtual int Id { get; set; }
        /// <summary>
        /// Идентификатор продукта в БД
        /// </summary>
        public virtual int ProductId { get; set; }
        /// <summary>
        /// Идентификатор заказчика в БД
        /// </summary>
        public virtual int CustomerId { get; set; }
        /// <summary>
        /// Количество товара в заказе
        /// </summary>
        public virtual int Amount
        {
            get { return m_amount; }
            set
            {
                m_amount = value; 
                OnPropertyChanged("Amount");
            }
        }

        /// <summary>
        /// Дата создания заказа
        /// </summary>
        public virtual DateTime Date { get; set; }
        /// <summary>
        /// Комментарий к заказу (дополнительные сведения)
        /// </summary>
        public virtual string Comment { get; set; }
        /// <summary>
        /// Идентификатор комментария в БД, по которому создается заказ
        /// </summary>
        public virtual int InitialVkCommentId { get; set; }

        public Order()
        {
            m_cachedProduct = null;
        }

        public Product GetOrderedProduct()
        {
            if (m_cachedProduct == null)
            {
                var repo = Core.Repositories.DbManger.GetInstance().GetProductRepository();
                m_cachedProduct = repo.GetById(ProductId);
            }

            return m_cachedProduct;
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
