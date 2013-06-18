using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VkShopManager.Annotations;

namespace VkShopManager.Domain
{
    public class Payment : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual int Id { get; set; }
        public virtual int PayerId { get; set; }
        public virtual int AlbumId { get; set; }
        public virtual decimal Amount { get; set; }
        public virtual DateTime Date { get; set; }
        public virtual string Comment { get; set; }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
