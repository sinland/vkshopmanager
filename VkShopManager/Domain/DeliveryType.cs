using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VkShopManager.Annotations;

namespace VkShopManager.Domain
{
    public class DeliveryType : INotifyPropertyChanged
    {
        /// <summary>
        /// Идентификатор в БД
        /// </summary>
        public virtual int Id { get; set; }
        /// <summary>
        /// Статус активности
        /// </summary>
        public virtual bool IsActive { get; set; }
        /// <summary>
        /// Комментарий к типу
        /// </summary>
        public virtual string Comment { get; set; }
        /// <summary>
        /// Стоимость доставки
        /// </summary>
        public virtual decimal Price { get; set; }

        public virtual bool IsConditional { get; set; }
        public virtual decimal MinimumOrderSummaryCondition { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Comment;
        }
    }
}
